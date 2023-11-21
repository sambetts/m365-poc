# react-office-dashboard
This is an application to show Microsoft Graph usage in a React/JavaScript application with MSAL. It reads emails and Teams chats of the logged in user and doesn't need any backend to work. 

Built with Node.js v16.17.0.

## Setup Application
[Register new Azure AD application](https://learn.microsoft.com/en-us/azure/active-directory-b2c/tutorial-register-spa#register-the-spa-application). Your application registration needs "Single Page Application" authentication configured with whatever you configure for "REACT_APP_MSAL_REDIRECT" below. No permissions need to be added to the application as we'll use delegated permissions as requested by the application. 

Next, create new file ".env.development" in same path as ".env". Copy/paste and change where needed these values:

```
PORT=44433
HTTPS=true
REACT_APP_MSAL_REDIRECT=https://localhost:44433
REACT_APP_MSAL_AUTHORITY=https://login.microsoftonline.com/{TENANT_GUID}
REACT_APP_MSAL_CLIENT_ID=fc228796-b256-4b2f-8563-3ff5f1121909
```

## Run Application

In the project directory, you can run:

### `npm start`

Runs the app in the development mode.\
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

## Learn More
To learn React, check out the [React documentation](https://reactjs.org/).
