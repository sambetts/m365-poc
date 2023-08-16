import React from 'react';

import TextField from '@mui/material/TextField';
import { Grid } from '@mui/material';
import { AppDetails } from '../models/WizardModels';
import { WizardButtons } from '../WizardButtons';

export const AppDetailsForm: React.FC<{ detailsDone: Function, cancel: Function }> = (props) => {

  const [nameShort, setNameShort] = React.useState<string>("");
  const [nameLong, setNameLong] = React.useState<string>("");

  const [descShort, setDescShort] = React.useState<string>("");
  const [descLong, setDescLong] = React.useState<string>("");

  const [companyName, setCompanyName] = React.useState<string>("");
  const [companyUrl, setCompanyUrl] = React.useState<string>("");

  const formSave = () => {

    // Validate but let default HTML5 validation handle errors
    let error = false;
    if(nameShort.length === 0 || nameLong.length === 0 || descShort.length === 0|| descLong.length === 0|| companyName.length === 0|| companyUrl.length === 0) error = true;
  
    var regexp = new RegExp('^[Hh][Tt][Tt][Pp][Ss]?://');
    if(!regexp.test(companyUrl))
      error = true;

    if (!error) {
      let details: AppDetails =
      {
        shortName: nameShort,
        longName: nameLong,
        longDescription: descLong,
        shortDescription: descShort,
        companyName: companyName,
        companyWebsite: companyUrl
      }

      props.detailsDone(details);
    }
  }

  return (
    <form autoComplete="off">
      <p>To make any Teams app, we need this required information to build the manifest:</p>

      <Grid container>
        <Grid>
          <h3>App names</h3>
          <p>A short name (30 characters or less) is required. Feel free to also include a longer version if your preferred name exceeds 30 characters.</p>
        </Grid>
        <Grid container>
          <TextField id="standard-basic" label="Short name" required size='small' className='field' inputProps={{ maxLength: 30 }} 
            value={nameShort} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setNameShort((e.target as HTMLInputElement).value)} />
          <TextField id="outlined-basic" label="Long name" required size='small' className='longField'
            value={nameLong} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setNameLong((e.target as HTMLInputElement).value)} />
        </Grid>

        <Grid style={{marginTop: 20}}>
          <h3>Descriptions</h3>
          <p>Include both short and full descriptions of your app. The short description must be under 80 characters and not repeated in the full description.</p>
        </Grid>
        <Grid container>
          <TextField id="standard-basic" label="Short description (80 characters or less)" required size='small' className='longField' inputProps={{ maxLength: 80 }}
            value={descShort} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setDescShort((e.target as HTMLInputElement).value)}/>
          <TextField id="outlined-basic" label="Full description (4000 characters or less)" required size='small' fullWidth  inputProps={{ maxLength: 4000 }}
            value={descLong} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setDescLong((e.target as HTMLInputElement).value)}
            style={{marginTop: 20}}/>
        </Grid>
        
        <Grid style={{marginTop: 20}}>
          <h3>Developer/company information</h3>
          <p>Enter your developer or company name and website. Make sure the website is a valid https URL.</p>
        </Grid>
        <Grid container>
          <TextField id="standard-basic" label="Developer/Company Name" required size='small' className='field' 
            value={companyName} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setCompanyName((e.target as HTMLInputElement).value)} />
          <TextField id="outlined-basic" label="Website" required type="url" size='small' className='longField' 
            value={companyUrl} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setCompanyUrl((e.target as HTMLInputElement).value)}/>
        </Grid>
        
      </Grid>

      <WizardButtons previousClicked={() => props.cancel()} previousText="Start Over" 
        nextClicked={() => formSave()} nextText="Save Info &amp; get app" />

    </form>
  );
};
