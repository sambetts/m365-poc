import { useMsal } from "@azure/msal-react";
import { IPublicClientApplication } from "@azure/msal-browser";
import { Button } from "react-bootstrap";

function handleLogin(instance: IPublicClientApplication, permissions: string[], onError: Function) {

    const loginRequest = {
        scopes: permissions
    }

    instance.loginPopup(loginRequest).catch((e : Error) => {
        console.error(e);
    });
}

/**
 * Renders a button which, when selected, will open a popup for login
 */
export const SignInButton = (props : {permissions: string[], onError: Function}) => {
    const { instance } = useMsal();

    return (
        <Button onClick={() => handleLogin(instance, props.permissions, props.onError)}>Sign into Azure AD</Button>
    );
}
