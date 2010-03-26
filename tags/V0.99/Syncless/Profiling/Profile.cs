﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using Syncless.Profiling.Exceptions;
namespace Syncless.Profiling
{
    public class Profile
    {
        private const string DEFAULT_DRIVE_NAME = "-";


        /// <summary>
        /// Profile name
        /// </summary>
        private string _profilename;
        /// <summary>
        /// Profile name
        /// </summary>
        public string ProfileName
        {
            get { return _profilename; }
            set { _profilename = value; }
        }

        /// <summary>
        /// Last Updated Time
        /// </summary>
        private long _lastUpdatedTime;
        /// <summary>
        /// Last Updated Time
        /// </summary>
        public long LastUpdatedTime
        {
            get { return _lastUpdatedTime; }
            set { _lastUpdatedTime = value; }
        }

        private List<ProfileDrive> _fullList;
        //Connected List of ProfileDrive
        private Dictionary<string, ProfileDrive> _logicalDict; //Key is LogicalId
        private Dictionary<string, ProfileDrive> _phyiscalDict; //key is PhysicalId
        public List<ProfileDrive> ProfileDriveList
        {
            get
            {
                List<ProfileDrive> driveList = new List<ProfileDrive>();
                driveList.AddRange(_fullList);
                return driveList;
            }

        }

        public Profile(string profileName)
        {
            _profilename = profileName;
            _fullList = new List<ProfileDrive>();
            _logicalDict = new Dictionary<string, ProfileDrive>();
            _phyiscalDict = new Dictionary<string, ProfileDrive>();
        }

        /// <summary>
        /// Find a Logical Mapping From a Physical Mapping
        /// </summary>
        /// <param name="physical"></param>
        /// <returns></returns>
        public string FindLogicalFromPhysical(string physical)
        {
            ProfileDrive drive = null;
            return _phyiscalDict.TryGetValue(physical, out drive) ? drive.LogicalId : null;
        }

        /// <summary>
        /// Find a Physical Mapping From a Logical Mapping
        /// </summary>
        /// <param name="logical">logical address</param>
        /// <returns>physical address . return null if the logical address is not found or the physical drive is not in.</returns>
        public string FindPhysicalFromLogical(string logical)
        {
            ProfileDrive drive = null;
            return _logicalDict.TryGetValue(logical, out drive) ? drive.PhysicalId : null;
        }

        /// <summary>
        /// Find a Logical Mapping From a Physical Mapping
        /// </summary>
        /// <param name="physical"></param>
        /// <returns></returns>
        public string FindLogicalIdFromGUID(string guid)
        {
            ProfileDrive drive = FindProfileDriveFromLogicalId(guid);
            return drive == null ? null : drive.LogicalId;
        }

        public string FindDriveNameFromLogical(string logical)
        {
            ProfileDrive drive = FindProfileDriveFromLogicalId(logical);
            return drive != null ? drive.DriveName : null;
        }

        public void SetDriveName(DriveInfo info, string name)
        {
            ProfileDrive drive = FindProfileDriveFromPhyisicalId(info.Name);
            drive.LastUpdated = DateTime.Now.Ticks;
            _lastUpdatedTime = DateTime.Now.Ticks;
            drive.DriveName = name;

        }
        /// <summary>
        /// Update a Particular drive with to its GUID
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="driveid"></param>
        public void InsertDrive(DriveInfo info, string guid)
        {
            ProfileDrive drive = null;
            string driveLetter = ProfilingHelper.ExtractDriveName(info.Name);
            if (!_phyiscalDict.TryGetValue(driveLetter, out drive))
            {
                drive = FindProfileDriveFromGUID(guid);
                if (drive == null)
                {
                    drive = CreateProfileDrive(guid);

                }
                drive.Info = info;
                _phyiscalDict[drive.PhysicalId] = drive;
                _logicalDict[drive.LogicalId] = drive;
            }
            else
            {
                if (drive.Guid.Equals(guid))
                {
                    throw new ProfileDriveConflictException();
                }
            }

        }
        public bool RemoveDrive(DriveInfo info)
        {
            string driveLetter = ProfilingHelper.ExtractDriveName(info.Name);
            ProfileDrive drive = null;
            if (_phyiscalDict.TryGetValue(driveLetter, out drive))
            {
                Debug.Assert(drive!=null);
                if (_phyiscalDict.ContainsKey(drive.PhysicalId))
                {
                    _phyiscalDict.Remove(drive.PhysicalId);
                }
                if (_logicalDict.ContainsKey(drive.LogicalId))
                {
                    _logicalDict.Remove(drive.LogicalId);
                }
                drive.Info = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public ProfileDrive CreateProfileDrive(string guid)
        {
            ProfileDrive drive = new ProfileDrive(guid, DEFAULT_DRIVE_NAME);
            _fullList.Add(drive);

            return drive;
        }
        internal bool AddProfileDrive(ProfileDrive drive)
        {
            this._fullList.Add(drive);
            return true;
        }
        internal ProfileDrive FindProfileDriveFromGUID(string guid)
        {
            foreach (ProfileDrive drive in _fullList)
            {
                if (drive.Guid.Equals(guid))
                {
                    return drive;
                }
            }
            return null;
        }
        internal ProfileDrive FindProfileDriveFromLogicalId(string logicalid)
        {
            foreach (ProfileDrive drive in _fullList)
            {
                if (drive.Guid.Equals(logicalid))
                {
                    return drive;
                }
            }
            return null;
        }
        internal ProfileDrive FindProfileDriveFromPhyisicalId(string physicalid)
        {
            if(_phyiscalDict.ContainsKey(physicalid)){
                return _phyiscalDict[physicalid];
            }
            return null;
        }


    }
}