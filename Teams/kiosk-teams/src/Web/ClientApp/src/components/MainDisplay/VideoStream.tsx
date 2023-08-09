import { VideoStreamRendererView } from "@azure/communication-calling";
import React from "react";

interface VideoProps
{ 
    videoStreamRendererView: VideoStreamRendererView,
    width: number,
    height: number
}

export const VideoStream = React.forwardRef<HTMLDivElement, VideoProps>((props) => {

    return <div style={{ height: props.height, width: props.width, backgroundColor: "black", position: "relative" }}>
        <div ref={ref => ref?.appendChild(props.videoStreamRendererView.target)} id="myVideo"
            style={{ backgroundColor: "black", position: "absolute", top: "50%", transform: "translateY(-50%)" }}>

        </div>
    </div>

});
