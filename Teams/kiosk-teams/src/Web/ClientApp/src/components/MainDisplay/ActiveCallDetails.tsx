import { Call, CallAgent, CallClient, LocalVideoStream, RemoteParticipant, RemoteVideoStream, VideoStreamRenderer, VideoStreamRendererView } from "@azure/communication-calling"
import React, { useEffect } from "react";
import { VideoStream } from "./VideoStream";

interface CallProps {
    callClient: CallClient,
    call: Call,
    callAgent: CallAgent,
    meeting: TeamsMeetingDetails
}

export const ActiveCallDetails: React.FC<CallProps> = (props) => {
    const [callState, setCallState] = React.useState<string>(props.call.state);

    // Renderers
    const [rendererRemote, setRendererRemote] = React.useState<VideoStreamRenderer | null>(null);
    const [rendererLocal, setRendererLocal] = React.useState<VideoStreamRenderer | null>(null);

    // Views 
    const [rendererViewRemote, setRendererViewRemote] = React.useState<VideoStreamRendererView | null>(null);
    const [localVideoStream, setLocalVideoStream] = React.useState<LocalVideoStream | null>(null);

    const enableWebcam = async (enableWebcam: boolean) => {

        if (enableWebcam) {

            const deviceManager = await props.callClient.getDeviceManager();
            const videoDevices = await deviceManager.getCameras();

            const videoDeviceInfo = videoDevices[0];
            const newLsV = new LocalVideoStream(videoDeviceInfo);
            setLocalVideoStream(newLsV);

            await props.call.startVideo(newLsV);
        }
        else {
            if (!localVideoStream) {
                alert('No local video stream?');
                return;
            }

            await props.call.stopVideo(localVideoStream);

            // Clean up local webcam renderer
            rendererLocal?.dispose();
            setRendererLocal(null);
            setLocalVideoStream(null);
        }
    }

    useEffect(() => {

        // Init video
        async function setupVideo() {

            // Init video for call
            subscribeToRemoteParticipantInCall(props.call);

            // Make sure we clean-up when someone leaves
            props.callAgent.on('callsUpdated', (e: any) => {
                e.removed.forEach(() => {
                    disposeRemoteVid();
                })
            });

            if (props.meeting.activateAcsClientWebCam) {
                enableWebcam(true);
            }
        }
        setupVideo();

        // Bind to call state changes to update our own state
        props.call.on("stateChanged", () => {
            setCallState(props.call.state);
        });
    }, [])

    const disposeRemoteVid = () => {
        rendererViewRemote?.dispose();
        setRendererViewRemote(null);
        rendererRemote?.dispose();
        setRendererRemote(null);
    }

    const handleVideoStream = (remoteVideoStream: RemoteVideoStream) => {
        remoteVideoStream.on('isAvailableChanged', async () => {
            if (remoteVideoStream.isAvailable) {
                remoteVideoView(remoteVideoStream);
            } else {
                disposeRemoteVid();
            }
        });
        if (remoteVideoStream.isAvailable) {
            remoteVideoView(remoteVideoStream);
        }
    }

    const subscribeToParticipantVideoStreams = (remoteParticipant: RemoteParticipant) => {
        remoteParticipant.on('videoStreamsUpdated', e => {
            e.added.forEach(v => {
                handleVideoStream(v);
            });
        });
        remoteParticipant.videoStreams.forEach(v => {
            handleVideoStream(v);
        });
    }

    const subscribeToRemoteParticipantInCall = (callInstance: Call) => {
        callInstance.on('remoteParticipantsUpdated', e => {
            e.added.forEach(p => {
                subscribeToParticipantVideoStreams(p);
            });
        });
        callInstance.remoteParticipants.forEach(p => {
            subscribeToParticipantVideoStreams(p);
        })
    }

    const remoteVideoView = async (remoteVideoStream: RemoteVideoStream) => {
        const remoteVidRenderer = new VideoStreamRenderer(remoteVideoStream);
        await setRendererRemote(remoteVidRenderer);
        const rendererRemoteViewNew = await remoteVidRenderer.createView();
        setRendererViewRemote(rendererRemoteViewNew);
    }

    return <div>
        
        {rendererViewRemote &&
            <div>
                <div>Remote Video</div>
                <VideoStream videoStreamRendererView={rendererViewRemote} 
                    ref={r => r?.appendChild(rendererViewRemote.target)} 
                    width={600} height={400}
                    />
            </div>
        }

        <table className="table">
            <tbody>
                <tr>
                    <td style={{width: 100}}><p className="font-weight-bold">Call State:</p></td>
                    <td>{callState}</td>
                </tr>
                <tr>
                    <td><p className="font-weight-bold">Meeting:</p></td>
                    <td><pre>{JSON.stringify(props.meeting)}</pre></td>
                </tr>
            </tbody>
        </table>
    </div>
}
