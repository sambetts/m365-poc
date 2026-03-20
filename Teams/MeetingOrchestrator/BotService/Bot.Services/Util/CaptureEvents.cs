// ***********************************************************************
// Assembly         : MeetingOrchestratorBot.Services
// 
// Created          : 09-07-2020
//

// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="CaptureEvents.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Skype.Bots.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using MeetingOrchestratorBot.Services.Media;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MeetingOrchestratorBot.Services.Util
{
    /// <summary>
    /// Class CaptureEvents.
    /// Implements the <see cref="MeetingOrchestratorBot.Services.Util.BufferBase{System.Object}" />
    /// </summary>
    /// <seealso cref="MeetingOrchestratorBot.Services.Util.BufferBase{System.Object}" />
    public class CaptureEvents : BufferBase<object>
    {
        /// <summary>
        /// The path
        /// </summary>
        private readonly string _path;
        /// <summary>
        /// The serializer
        /// </summary>
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaptureEvents" /> class.

        /// </summary>
        /// <param name="path">The path.</param>
        public CaptureEvents(string path)
        {
            _path = path;
            _serializer = new JsonSerializer();
        }

        /// <summary>
        /// Saves the json file.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileName">Name of the file.</param>
        private async Task saveJsonFile(Object data, string fileName)
        {
            Directory.CreateDirectory(_path);

            var name = fileName;
            var fullName = Path.Combine(_path, name);

            using (var stream = File.Open(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (var sw = new StreamWriter(stream))
                {
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jw.Formatting = Formatting.Indented;
                        _serializer.Serialize(jw, data);
                        await jw.FlushAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Saves the bson file.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileName">Name of the file.</param>
        private async Task saveBsonFile(Object data, string fileName)
        {
            Directory.CreateDirectory(_path);

            var name = fileName;
            var fullName = Path.Combine(_path, name);

            var file = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            using (var bson = new BsonDataWriter(file))
            {
                _serializer.Serialize(bson, data);
                await bson.FlushAsync();
            }
        }

        /// <summary>
        /// Saves the audio media buffer.
        /// </summary>
        /// <param name="data">The data.</param>
        private async Task _saveAudioMediaBuffer(SerializableAudioMediaBuffer data)
        {
            await saveBsonFile(data, data.Timestamp.ToString());
        }

        /// <summary>
        /// Saves the requests.
        /// </summary>
        /// <param name="data">The data.</param>
        private async Task _saveRequests(string data)
        {
            Directory.CreateDirectory(_path);

            var name = DateTime.UtcNow.Ticks.ToString();
            var fullName = Path.Combine(_path, name);

            byte[] encodedText = Encoding.Unicode.GetBytes(data);

            using (FileStream sourceStream = new FileStream($"{fullName}.json", FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        /// <summary>
        /// Processes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        protected override async Task Process(object data)
        {
            switch (data)
            {
                case string d:
                    await _saveRequests(d);
                    return;
                case SerializableAudioMediaBuffer d:
                    await _saveAudioMediaBuffer(d);
                    return;
                default:
                    return;
            }
        }

        /// <summary>
        /// Finalises this instance.
        /// </summary>
        public async Task Finalise()
        {
            // drain the un-processed buffers on this object
            while (Buffer.Count > 0)
            {
                await Task.Delay(200);
            }
            await End();
        }
    }
}
