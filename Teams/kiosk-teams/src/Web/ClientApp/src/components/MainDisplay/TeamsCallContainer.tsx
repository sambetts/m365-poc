import React from "react";
import { AzureKeyCredential } from '@azure/core-auth';
import { CommunicationIdentityClient } from "@azure/communication-identity";
import { CommunicationUserIdentifier } from "@azure/communication-common";
import { TeamsCallUI } from "./TeamsCallUI";
import { CallCompositeOptions } from "@azure/communication-react";

export const TeamsCallContainer: React.FC<{ config: ServiceConfiguration, meeting: TeamsMeetingDetails }> = (props) => {
    const [communicationUserIdentifier, setCommunicationUserIdentifier] = React.useState<CommunicationUserIdentifier | undefined>();
    const [userToken, setUserToken] = React.useState<string | undefined>();

    const kioskModeOps: CallCompositeOptions =
    {
        callControls:
        {
            cameraButton: false,
            devicesButton: false, 
            microphoneButton: false,
            peopleButton: false,
            screenShareButton: false
        }
    };

    React.useEffect(() => {

        // Start new call for configured teams meeting
        const azKeyCredential = new AzureKeyCredential(props.config.acsAccessKeyVal);
        const identityClient = new CommunicationIdentityClient(props.config.acsEndpointVal, azKeyCredential)

        identityClient.createUserAndToken(["voip"]).then((userAndToken) => {

            setCommunicationUserIdentifier(userAndToken.user);
            setUserToken(userAndToken.token);
        });

    }, []);

    return <>
        {communicationUserIdentifier && userToken &&
            <TeamsCallUI userId={communicationUserIdentifier} displayName={props.config.clientLocationInfo.name}
                locator={props.meeting.joinUrl} token={userToken} options={kioskModeOps} />}
    </>
}
