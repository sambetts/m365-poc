import { useMsal } from "@azure/msal-react";
import { IPublicClientApplication } from "@azure/msal-browser";

function handleLogout(instance: IPublicClientApplication) {
    instance.logoutPopup({
        mainWindowRedirectUri: "/"
    }).catch((e: Error) => {
        console.error(e);
    });
}

/**
 * Renders a button which, when selected, will open a popup for logout
 */
export const SignOutButton = () => {
    const { instance } = useMsal();

    return (
        <button 
            onClick={() => handleLogout(instance)}
            style={{
                padding: '0.5rem 1rem',
                backgroundColor: '#fff',
                color: '#333',
                border: '1px solid #ccc',
                borderRadius: '4px',
                cursor: 'pointer',
                fontSize: '0.9rem',
                fontWeight: '500',
                transition: 'all 0.2s ease'
            }}
            onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = '#f0f0f0';
                e.currentTarget.style.borderColor = '#999';
            }}
            onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#fff';
                e.currentTarget.style.borderColor = '#ccc';
            }}
        >
            Sign Out
        </button>
    );
}
