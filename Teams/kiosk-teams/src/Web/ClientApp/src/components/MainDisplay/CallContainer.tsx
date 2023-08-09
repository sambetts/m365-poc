import { Call, CallAgent, CallClient } from "@azure/communication-calling"
import React from "react";
import { ActiveCallDetails } from "./ActiveCallDetails";
import { AzureKeyCredential } from '@azure/core-auth';
import { CommunicationIdentityClient } from "@azure/communication-identity";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";
import { JoinCallOptions } from "@azure/communication-calling/types/communication-calling";

export const CallContainer: React.FC<{ config: ServiceConfiguration, meeting: TeamsMeetingDetails }> = (props) => {

    const [activeCall, setActiveCall] = React.useState<Call | null>(null);
    const [callAgent, setCallAgent] = React.useState<CallAgent | null>(null);
    const [callClient] = React.useState<CallClient>(new CallClient());

    React.useEffect(() => {

        // Start new call for configured teams meeting
        const azKeyCredential = new AzureKeyCredential(props.config.acsAccessKeyVal);
        const identityClient = new CommunicationIdentityClient(props.config.acsEndpointVal, azKeyCredential)

        identityClient.createUserAndToken(["voip"]).then((userAndToken) => {
            const tokenCredential = new AzureCommunicationTokenCredential(userAndToken.token);

            callClient.createCallAgent(tokenCredential, { displayName: props.config.clientLocationInfo.name }).then(agent => {
                setCallAgent(agent);

                const joinOptions : JoinCallOptions = 
                {
                    audioOptions: {
                        muted: true
                    }
                };
                const newCall = agent.join({ meetingLink: props.meeting.joinUrl,  }, joinOptions);
                setActiveCall(newCall);
            });
        });

    }, []);

    return <div>
        {!activeCall ?
            <>
                <p>Connecting to Teams Meeting...one sec...</p>
            </>
            :
            <>
                {activeCall.state === "Disconnected" ?
                    <div>The call has ended</div>
                    :
                    <>
                        {callAgent &&
                            <ActiveCallDetails call={activeCall} callClient={callClient} callAgent={callAgent} meeting={props.meeting} />
                        }
                    </>
                }

            </>
        }

    </div>
}
