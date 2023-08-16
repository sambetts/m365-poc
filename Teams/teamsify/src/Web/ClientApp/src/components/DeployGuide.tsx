import React from 'react';

export const DeployGuide: React.FC<{}> = () => {


  return (
    <div>
      <h3>Quick Start Guide</h3>
      <p>Once you've completed wizard here and downloaded your app file, you need to deploy to Teams and distribute to your users. For this you'll need Teams administrator rights</p>
      <p>From the <a href='https://docs.microsoft.com/en-us/microsoftteams/manage-teams-in-modern-portal'>Teams Admin center</a>, find "Manage Apps" and upload your zip file.</p>
      <img src='imgs/Upload1.jpg' alt='Upload zip' />
      <p>You can edit the manifest file in the zip if you'd like to change any details too.</p>
      <p>Once uploaded, click the link to see it in your app store:</p>
      <img src='imgs/Upload2.jpg' alt='Go to manage uploaded app page' />

      <p>This is the information in the manifest.json file, in the zip file generated.</p>
      <img src='imgs/Upload3.jpg' alt='Manage app' />

      <p>Now we just need to publish it to the organisation with a "setup policy". 
        Edit an existing policy - "Global" is a default policy for everyone.</p>
      <img src='imgs/Upload4.jpg' alt='Edit setup policy' />

      <p>Once in a setup policy, add your newly added app so users get it pre-installed:</p>
      <img src='imgs/Upload5.jpg' alt='Add app to setup policy' />
      <p>Click "Add".</p>

      <p>Finally, you can pin the app so it always appears on the left-hand side:</p>
      <img src='imgs/Upload6.jpg' alt='Pin app to Teams launcher' />
      <p>More information: <a href='https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/deploy-and-publish/apps-publish-overview'>Distribute your Microsoft Teams app</a></p>
    </div>
  );
};
