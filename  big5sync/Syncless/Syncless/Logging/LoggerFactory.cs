﻿/*
 * 
 * Author: Koh Cher Guan
 * 
 */

using System.Diagnostics;
using Syncless.Core;

namespace Syncless.Logging
{
    /// <summary>
    /// This factory helps to create the respective logger.
    /// </summary>
    public class LoggerFactory
    {
        /// <summary>
        /// Create and return the specified type of logger
        /// </summary>
        /// <param name="type">A <see cref="string" /> specifying the type of logger.</param>
        /// <returns></returns>
        public static Logger CreateLogger(string type)
        {
            if (type.Equals(ServiceLocator.USER_LOG))
            {
                return CreateUserLog();
            }
            else if (type.Equals(ServiceLocator.DEBUG_LOG))
            {
                return CreateDebugLog();
            }
            else if (type.Equals(ServiceLocator.DEVELOPER_LOG))
            {
                return CreateDeveloperLog();
            }
            else
            {
                Debug.Assert(false);
                return null;
            }
        }
        
        private static Logger CreateUserLog()
        {
            return new UserLogger();
        }

        private static Logger CreateDebugLog()
        {
            return new DebugLogger();
        }

        private static Logger CreateDeveloperLog()
        {
            return new DeveloperLogger();
        }
    }
}
