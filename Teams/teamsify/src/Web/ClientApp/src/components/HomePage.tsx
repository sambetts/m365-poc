import React from 'react';
import { SelectSite } from './TeamifyWeb/SelectSite';
import { SitePreview } from './TeamifyWeb/SitePreview';
import { AppDetailsForm } from './TeamifyWeb/AppDetailsForm';
import { AppDownload } from './TeamifyWeb/AppDownload';
import { AppDetails } from './models/WizardModels';
import { Stage } from './models/enums';
import { Button, Grid } from '@mui/material';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import BackupOutlinedIcon from '@mui/icons-material/BackupOutlined';
import FormatListNumberedRtlOutlinedIcon from '@mui/icons-material/FormatListNumberedRtlOutlined';

interface HomeProps
{ 
  wizardStageChange: Function, 
  setAppDetails: Function, 
  appDetails: AppDetails | null,
  sessionId: string | null, 
  setSessionId : Function
}

export const HomePage: React.FC<HomeProps> = (props) => {

  const [currentStage, setCurrentStage] = React.useState<Stage>(Stage.Home);
  const [url, setUrl] = React.useState<string>("");


  const siteSelect = (url: string, sessionId: string) => {
    setUrl(url);
    props.setSessionId(sessionId);
    setStage(Stage.VerifySite);
  }

  const appDetailsSet = (details: AppDetails) => {
    props.setAppDetails(details);
    setStage(Stage.Download);
  }

  const setStage = (stage: Stage) => {
    setCurrentStage(stage);
    props.wizardStageChange(stage);
  }


  const renderSwitch = (stage: Stage) => {
    switch (stage) {
      case Stage.Home:
        return (
          <div>
            <p>Add &amp; pin your website to Teams easily with Teamsify.</p>
            <img src='imgs/example.png' alt='Example Teamsified Website' />
            <p>Get your website added to Teams in 3 easy steps. 5 minutes tops.</p>

            <Grid container justifyContent="center">

              <table className='processInfoGraphic'>
                <tbody>
                  <tr>
                    <td className='icon'><FormatListNumberedRtlOutlinedIcon fontSize='large' /></td>
                    <td className='stepHeader'>Configure</td>

                    <td className='icon'><FileDownloadOutlinedIcon fontSize='large' /></td>
                    <td className='stepHeader'>Download</td>

                    <td className='icon'><BackupOutlinedIcon fontSize='large' /></td>
                    <td className='stepHeader'>Deploy</td>
                  </tr>
                  <tr>
                    <td colSpan={2} className="description">Configure your website for Teams</td>
                    <td colSpan={2} className="description">Download Teams app generated</td>
                    <td colSpan={2} className="description">Deploy to your Office 365/Teams tenant</td>
                  </tr>
                </tbody>
              </table>
            </Grid>
            <Grid container justifyContent="center" style={{ marginTop: 20 }}>
              <Button variant="contained" size="large" onClick={() => setStage(Stage.SiteSelection)}>Start Teamsify Wizard</Button>
            </Grid>

            {props.appDetails !== null &&

              <Grid container justifyContent="center" style={{ marginTop: 20 }}>
                <Button variant="outlined" size="large" onClick={() => setStage(Stage.Download)}>Download Last Created App</Button>
              </Grid>
            }

          </div>);

      case Stage.SiteSelection:
        return <SelectSite siteSelected={(url: string, sessionId: string) => siteSelect(url, sessionId)} />;
      case Stage.VerifySite:
        return <SitePreview url={url} siteConfirmed={() => setStage(Stage.EnterData)}
          siteCancel={() => setStage(Stage.Home)} />
      case Stage.EnterData:
        return <AppDetailsForm detailsDone={(details: AppDetails) => appDetailsSet(details)} cancel={() => setStage(Stage.Home)} />
      case Stage.Download:
        if (props.sessionId)
          return <AppDownload details={props.appDetails!} url={url}
            startOver={() => setStage(Stage.Home)}
            goBack={() => setStage(Stage.EnterData)} sessionId={props.sessionId} />
        else
          return <p>Invalid session</p>
      default:
        return <p>No idea what to display</p>;
    }
  }

  return (
    <div>
      {renderSwitch(currentStage)}


    </div>
  );
};
