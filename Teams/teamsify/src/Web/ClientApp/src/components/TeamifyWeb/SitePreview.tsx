import React from 'react';

import { WizardButtons } from '../WizardButtons';
import { Button } from '@mui/material';

export const SitePreview: React.FC<{ url: string, siteConfirmed: Function, siteCancel: Function }> = (props) => {
  const [showSiteSetupRequirements, setShowSiteSetupRequirements] = React.useState<boolean>(false);
  const contentRef = React.useRef<HTMLIFrameElement>(null)

  return (
    <>
      {!showSiteSetupRequirements ?
        <>
          <div id='webPreview'>

            <img src='imgs/TeamsPreview.png' alt='Teams preview' />
            <div className='previewWindow'>
              <iframe src={props.url} ref={contentRef} title="Preview" />
            </div>

            <div style={{ paddingLeft: 32 }}>
              <WizardButtons nextClicked={() => props.siteConfirmed()} nextText="Looks Good - Build the App"
                previousText="Page Isn't Loading" previousClicked={() => setShowSiteSetupRequirements(true)} />
            </div>
            
          </div>
        </>
        :
        <>
          <h3>Website Incompatible with Teams Personal Tabs</h3>
          <p>Alas, this website isn't configured to allow it to be shown in Teams personal tabs (or any iframe).</p>
          <p>You need to add this HTTP header to your website responses, for at least the page '{props.url}':</p>

          <pre className='httpHeader'>Content-Security-Policy: frame-ancestors teams.microsoft.com *.teams.microsoft.com *.skype.com teamsify.app</pre>

          <p>That tells the browser you're looking at right now that your website can be loaded in an IFrame, from those domains.</p>
          <p>More info on requirements: <a href='https://docs.microsoft.com/en-us/microsoftteams/platform/tabs/how-to/tab-requirements'>https://docs.microsoft.com/en-us/microsoftteams/platform/tabs/how-to/tab-requirements</a></p>
          <Button variant="outlined" size="large" onClick={() => props.siteCancel()}>Go Back</Button>
        </>
      }


    </>

  );
};
