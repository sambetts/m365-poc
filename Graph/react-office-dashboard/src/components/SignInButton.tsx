import { useMsal } from "@azure/msal-react";
import { IPublicClientApplication } from "@azure/msal-browser";

function handleLogin(instance: IPublicClientApplication, permissions: string[]) {

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
export const SignInButton = (props : {permissions: string[]}) => {
    const { instance } = useMsal();

    return (
        <button onClick={() => handleLogin(instance, props.permissions)}>Sign into Azure AD</button>
    );
}
