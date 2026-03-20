namespace Bot.Services.Bot
{
    using global::Bot.Services.Util;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Models;
    using Microsoft.Skype.Bots.Media;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages video socket subscriptions for call participants.
    /// Uses an LRU cache to prioritize active speakers when all sockets are occupied.
    /// </summary>
    internal class VideoSubscriptionManager
    {
        private readonly HashSet<uint> _availableSocketIds = new();
        private readonly LRUCache _currentVideoSubscriptions = new(SampleConstants.NumberOfMultiviewSockets + 1);
        private readonly ConcurrentDictionary<uint, uint> _msiToSocketIdMapping = new();
        private readonly object _subscriptionLock = new();
        private readonly BotMediaStream _mediaStream;
        private readonly int _videoSocketCount;
        private readonly IGraphLogger _logger;
        private readonly string _callId;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoSubscriptionManager"/> class.
        /// </summary>
        /// <param name="mediaStream">The bot media stream for issuing subscribe/unsubscribe calls.</param>
        /// <param name="videoSockets">The video sockets available for subscriptions.</param>
        /// <param name="logger">The graph logger.</param>
        /// <param name="callId">The call identifier used for log messages.</param>
        public VideoSubscriptionManager(
            BotMediaStream mediaStream,
            IReadOnlyList<IVideoSocket> videoSockets,
            IGraphLogger logger,
            string callId)
        {
            _mediaStream = mediaStream;
            _videoSocketCount = videoSockets.Count;
            _logger = logger;
            _callId = callId;

            foreach (var socket in videoSockets)
            {
                _availableSocketIds.Add((uint)socket.SocketId);
            }
        }

        /// <summary>
        /// Subscribe to a participant's video and VBSS streams.
        /// When <paramref name="forceSubscribe"/> is true, the least recently used
        /// socket is released if no sockets are available.
        /// </summary>
        /// <param name="participant">The participant to subscribe to.</param>
        /// <param name="forceSubscribe">If true, evicts the LRU subscription when sockets are full.</param>
        public void SubscribeToParticipantVideo(IParticipant participant, bool forceSubscribe = true)
        {
            bool subscribeToVideo = false;
            uint socketId = uint.MaxValue;

            var participantSendCapableVideoStream = participant.Resource.MediaStreams.Where(x => x.MediaType == Modality.Video &&
               (x.Direction == MediaDirection.SendReceive || x.Direction == MediaDirection.SendOnly)).FirstOrDefault();
            if (participantSendCapableVideoStream != null)
            {
                bool updateMSICache = false;
                var msi = uint.Parse(participantSendCapableVideoStream.SourceId);
                lock (_subscriptionLock)
                {
                    if (_currentVideoSubscriptions.Count < _videoSocketCount)
                    {
                        if (!_msiToSocketIdMapping.ContainsKey(msi))
                        {
                            if (_availableSocketIds.Any())
                            {
                                socketId = _availableSocketIds.Last();
                                _availableSocketIds.Remove((uint)socketId);
                                subscribeToVideo = true;
                            }
                        }

                        updateMSICache = true;
                        _logger.Info($"[{_callId}:SubscribeToParticipant(socket {socketId} available, the number of remaining sockets is {_availableSocketIds.Count}, subscribing to the participant {participant.Id})");
                    }
                    else if (forceSubscribe)
                    {
                        updateMSICache = true;
                        subscribeToVideo = true;
                    }

                    if (updateMSICache)
                    {
                        _currentVideoSubscriptions.TryInsert(msi, out uint? dequeuedMSIValue);
                        if (dequeuedMSIValue != null)
                        {
                            _msiToSocketIdMapping.TryRemove((uint)dequeuedMSIValue, out socketId);
                        }
                    }
                }

                if (subscribeToVideo && socketId != uint.MaxValue)
                {
                    _msiToSocketIdMapping.AddOrUpdate(msi, socketId, (k, v) => socketId);

                    _logger.Info($"[{_callId}:SubscribeToParticipant(subscribing to the participant {participant.Id} on socket {socketId})");
                    _mediaStream.Subscribe(MediaType.Video, msi, VideoResolution.HD1080p, socketId);
                }
            }

            // vbss viewer subscription
            var vbssParticipant = participant.Resource.MediaStreams.SingleOrDefault(x => x.MediaType == Modality.VideoBasedScreenSharing
            && x.Direction == MediaDirection.SendOnly);
            if (vbssParticipant != null)
            {
                _logger.Info($"[{_callId}:SubscribeToParticipant(subscribing to the VBSS sharer {participant.Id})");
                _mediaStream.Subscribe(MediaType.Vbss, uint.Parse(vbssParticipant.SourceId), VideoResolution.HD1080p, socketId);
            }
        }

        /// <summary>
        /// Unsubscribe and free up the video socket for the specified participant.
        /// </summary>
        /// <param name="participant">The participant to unsubscribe.</param>
        public void UnsubscribeFromParticipantVideo(IParticipant participant)
        {
            var participantSendCapableVideoStream = participant.Resource.MediaStreams.Where(x => x.MediaType == Modality.Video &&
              (x.Direction == MediaDirection.SendReceive || x.Direction == MediaDirection.SendOnly)).FirstOrDefault();

            if (participantSendCapableVideoStream != null)
            {
                var msi = uint.Parse(participantSendCapableVideoStream.SourceId);
                lock (_subscriptionLock)
                {
                    if (_currentVideoSubscriptions.TryRemove(msi))
                    {
                        if (_msiToSocketIdMapping.TryRemove(msi, out uint socketId))
                        {
                            _mediaStream.Unsubscribe(MediaType.Video, socketId);
                            _availableSocketIds.Add(socketId);
                        }
                    }
                }
            }
        }
    }
}
