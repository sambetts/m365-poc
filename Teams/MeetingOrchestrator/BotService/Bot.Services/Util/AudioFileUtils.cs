// ***********************************************************************
// Assembly         : MeetingOrchestratorBot.Services
// 
// Created          : 09-08-2020
//

// Last Modified On : 09-09-2020
// ***********************************************************************
// <copyright file="AudioFileUtils.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.IO;

namespace MeetingOrchestratorBot.Services.Util
{
    /// <summary>
    /// Class AudioFileUtils.
    /// </summary>
    public static class AudioFileUtils
    {
        /// <summary>
        /// Creates the file path.
        /// </summary>
        /// <param name="rootFolder">The root folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>System.String.</returns>
        public static string CreateFilePath(string rootFolder, string fileName)
        {
            var path = Path.Combine(rootFolder, fileName);
            var fInfo = new FileInfo(path);
            if (fInfo.Directory != null && !fInfo.Directory.Exists)
            {
                fInfo.Directory.Create();
            }

            return path;
        }
    }
}
