﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syncless.CompareAndSync.CompareObject;
using Syncless.CompareAndSync.Request;
using Syncless.CompareAndSync.Visitor;
using Syncless.Filters;

namespace Syncless.CompareAndSync
{
    public class CompareAndSyncController
    {
        private static CompareAndSyncController _instance;

        public static CompareAndSyncController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CompareAndSyncController();
                }
                return _instance;
            }
        }

        private CompareAndSyncController()
        {            
        }

        public RootCompareObject Sync(ManualSyncRequest request)
        {
            List<Filter> filters = request.Filters.ToList<Filter>();
            filters.Add(new SynclessArchiveFilter(request.Config.ArchiveName));

            RootCompareObject rco = new RootCompareObject(request.Paths);
            CompareObjectHelper.PreTraverseFolder(ref rco, new BuilderVisitor(filters));
            CompareObjectHelper.PreTraverseFolder(ref rco, new XMLMetadataVisitor());
            CompareObjectHelper.PostTraverseFolder(ref rco, new ComparerVisitor());
            CompareObjectHelper.PreTraverseFolder(ref rco, new SyncerVisitor(request.Config));
            CompareObjectHelper.PreTraverseFolder(ref rco, new XMLWriterVisitor());
            return rco;
        }

        public RootCompareObject Compare(ManualCompareRequest request)
        {
            RootCompareObject rco = new RootCompareObject(request.Paths);
            CompareObjectHelper.PreTraverseFolder(ref rco, new BuilderVisitor(request.Filters));
            CompareObjectHelper.PreTraverseFolder(ref rco, new XMLMetadataVisitor());
            CompareObjectHelper.PostTraverseFolder(ref rco, new ComparerVisitor());
            return rco;
        }

        public void Sync(AutoSyncRequest request)
        {
            SeamlessQueueControl.Instance.AddSyncJob(request);
        }

        public void PrepareForTermination()
        {
            
        }
    }
}
