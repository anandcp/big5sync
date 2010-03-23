﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Syncless.Profiling.Exceptions;
using Syncless.Helper;
using Syncless.Core;
using Syncless.Notification;
namespace Syncless.Profiling
{
    public class ProfilingLayer
    {
        public const string RELATIVE_PROFILING_SAVE_PATH = ".syncless\\profiling.xml";
        public const string RELATIVE_GUID_SAVE_PATH = ".syncless\\guid.id";
        public const string RELATIVE_PROFILING_ROOT_SAVE_PATH = "profiling.xml";


        #region Singleton/Profile
        private static ProfilingLayer _instance;
        public static ProfilingLayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProfilingLayer();
                }
                return _instance;
            }
        }
        private Profile _profile;
        private ProfilingLayer()
        {
            _profile = new Profile("");
        }
        public void ChangeProfileName(string newName)
        {
            _profile.ProfileName = newName;
        }
        public Profile CurrentProfile
        {
            get { return _profile; }
        }
        #endregion

        #region conversion methods
        /// <summary>
        /// Convert a Logical address to a Physical address
        ///    will replace 001:/Lectures with C:/Lectures
        /// </summary>
        /// <param name="path">The Logical Address to be converted</param>
        /// <returns>The Physical Address</returns>
        public string ConvertLogicalToPhysical(string path)
        {
            string logicalid = ProfilingHelper.ExtractDriveName(path);
            string driveid = GetDriveFromLogicalId(logicalid);
            if (driveid == null)
            {
                return null;
            }
            string relativepath = ProfilingHelper.ExtractRelativePath(path);
            return driveid + ":" + relativepath;

        }
        /// <summary>
        /// Convert a Physical address to a Logical address
        ///    will replace C:/Lectures with 001:/Lectures
        /// </summary>
        /// <param name="path">The Physical Address to be converted</param>
        /// <returns>The Logical Address</returns>
        public string ConvertPhysicalToLogical(string path, bool create)
        {
            string driveid = ProfilingHelper.ExtractDriveName(path);
            string logicalid = GetLogicalIdFromDrive(driveid);
            if (logicalid == null)
            {
                if (!create)
                {
                    return null;
                }
                else
                {
                    string guid = ProfilingGUIDHelper.GetGUID(driveid);
                    //try to find a mapping
                    _profile.InsertDrive(new DriveInfo(driveid), guid);

                    logicalid = guid;
                }
            }
            string relativepath = ProfilingHelper.ExtractRelativePath(path);
            return logicalid + ":" + relativepath;
        }
        /// <summary>
        /// Take in a list of logical address , convert them to physical and return only those that are currently available.
        /// </summary>
        /// <param name="pathList">The list of logical address to convert</param>
        /// <returns></returns>
        public List<string> ConvertAndFilterToPhysical(List<string> pathList)
        {
            List<string> convertedPathList = new List<string>();

            foreach (string path in pathList)
            {
                string convertedPath = ConvertLogicalToPhysical(path);
                if (convertedPath != null)
                {
                    convertedPathList.Add(convertedPath);
                }
            }
            return convertedPathList;
        }
        /// <summary>
        /// Take in a list of logical address , convert them to physical and return 2 list of address
        ///   first list are the converted paths
        ///   second list are the unconverted path
        /// </summary>
        /// <param name="pathList">The list of logical address to convert</param>
        /// <returns>two array of path. string[0] -> converted path , string[1] -> unconverted path.</returns>
        public List<string>[] ConvertAndFilter(List<string> pathList)
        {
            List<string> convertedPathList = new List<string>();
            List<string> unconvertedPathList = new List<string>();
            foreach (string path in pathList)
            {
                string convertedPath = ConvertLogicalToPhysical(path);
                if (convertedPath != null)
                {
                    convertedPathList.Add(PathHelper.RemoveTrailingSlash(convertedPath));
                }
                else
                {
                    unconvertedPathList.Add(PathHelper.RemoveTrailingSlash(path));
                }

            }

            return new List<string>[2] { convertedPathList, unconvertedPathList };
        }

        /// <summary>
        /// Get the Logical Id for a Drive.
        /// </summary>
        /// <param name="info">The Drive</param>
        /// <returns>The logical Id Assigned to the drive. null - if drive is not assigned.</returns>
        public string GetLogicalIdFromDrive(DriveInfo info)
        {
            Debug.Assert(info != null);
            String name = info.Name.Substring(0, info.Name.IndexOf(":"));
            return GetLogicalIdFromDrive(name);
        }
        /// <summary>
        /// Get the Logical Id for a Drive.
        /// </summary>
        /// <param name="name">The name of the drive</param>
        /// <returns></returns>
        public string GetLogicalIdFromDrive(string name)
        {
            Debug.Assert(_profile != null);
            return _profile.FindLogicalFromPhysical(name);
        }
        /// <summary>
        /// get the Drive from Logical Id
        /// </summary>
        /// <param name="id">The Logical Id</param>
        /// <returns>The Name of the Drive (i.e 'C')</returns>
        public String GetDriveFromLogicalId(string id)
        {
            Debug.Assert(id != null);
            Debug.Assert(_profile != null);
            return _profile.FindPhysicalFromLogical(id);
        }
        #endregion

        /// <summary>
        /// Merge a profiling.xml from a path to the current profile.
        /// </summary>
        /// <param name="path"></param>
        public void Merge(string path)
        {
            if (File.Exists(path))
            {
                List<Profile> profileList = ProfilingXMLHelper.LoadProfile(path);
                ProfileMerger.Merge(_profile, profileList);
            }
        }
        /// <summary>
        /// Save all the profiling xml to all the various Location.
        /// </summary>
        /// <returns>true if the save is complete.</returns>
        public void SaveTo(List<string> savedLocation)
        {

            ProfilingXMLHelper.SaveProfile(_profile, savedLocation[0]);

        }
        /// <summary>
        /// Initialize the Profiling Layer.
        /// </summary>
        /// <param name="path">The root path for the profiling configuration file.</param>
        /// <returns>true if the profile is load.</returns>

        public bool Init(List<string> paths)
        {
            string path = paths[0];
            try
            {
                Profile p = ProfilingXMLHelper.LoadSingleProfile(path);
                Debug.Assert(p != null);
                _profile = p;
            }
            catch (FileNotFoundException)
            {
                Profile profile = ProfilingXMLHelper.CreateDefaultProfile(path);
                _profile = profile;
                //Since the profile is newly created , no need to traverse all the drive
            }
            for (int i = 1; i < paths.Count; i++)
            {

                if (File.Exists(paths[i]))
                {
                    List<Profile> profileList = ProfilingXMLHelper.LoadProfile(paths[i]);
                    ProfileMerger.Merge(_profile, profileList);
                }
            }

            DriveInfo[] driveList = DriveInfo.GetDrives();

            foreach (DriveInfo driveinfo in driveList)
            {
                UpdateDrive(driveinfo);
            }
            return true;
        }
        /// <summary>
        /// Load only the default saved location
        /// </summary>
        /// <param name="path"></param>
        public void Init(string path)
        {
            try
            {
                Profile p = ProfilingXMLHelper.LoadSingleProfile(path);
                Debug.Assert(p != null);
                _profile = p;
                
            }
            catch (FileNotFoundException)
            {
                Profile profile = ProfilingXMLHelper.CreateDefaultProfile(path);
                _profile = profile;                
            }
            if (_profile.ProfileName.Equals(ProfilingXMLHelper.DEFAULT_NAME))
            {
                ServiceLocator.UIPriorityQueue().Enqueue(new NewProfileNotification());
            }
            else
            {
                SetupDrives();
            }


        }
        public bool SetProfileName(string name)
        {   
            _profile.ProfileName = name;
            SetupDrives();
            return true;
        }
        private void SetupDrives()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo d in drives)
            {
                string guid = d.Name + @"\" + @".syncless\guid.id";
                if (File.Exists(guid))
                {
                    //if drive contain guid.
                    string profilingxml = d.Name + @"\" + @".syncless\profiling.xml";
                    if (File.Exists(profilingxml))
                    {
                        Profile p = ProfilingXMLHelper.LoadSingleProfile(profilingxml, _profile.ProfileName);
                        ProfileMerger.Merge(_profile, p);
                    }
                }
                UpdateDrive(d);
            }

        }
        /// <summary>
        /// Update a Drive with it guid and create a mapping in the profile.
        ///   used when a device is plugin or when startup.
        /// </summary>
        /// <param name="driveinfo">The drive to update</param>
        /// <returns>true if drive has a guid. False if it does not.</returns>
        public bool UpdateDrive(DriveInfo driveinfo)
        {
            FileInfo info = new FileInfo(driveinfo.RootDirectory.Name + RELATIVE_GUID_SAVE_PATH);
            if (info.Exists)
            {
                string guid = ProfilingGUIDHelper.ReadGUID(info);
                string driveid = ProfilingHelper.ExtractDriveName(info.FullName);
                _profile.InsertDrive(driveinfo, guid);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Remove a drive from a mapping. Used when a device is pluged out.
        /// </summary>
        /// <param name="driveinfo">The Drive that was plug out</param>
        /// <returns>true if the drive is being removed.</returns>
        public bool RemoveDrive(DriveInfo driveinfo)
        {
            return _profile.RemoveDrive(driveinfo);
        }

    }
}
