
import '../NavMenu.css';
import React, { ChangeEvent } from 'react';
import { Box, Button, Checkbox, FormControl, FormControlLabel, FormGroup, InputLabel, OutlinedInput, TextField } from "@mui/material";
import { getTeamsMeetingDetailsParam } from '../../engine/TeamsMeetingUrlParser';

export const GenerateUrl: React.FC<{}> = () => {

  const [joinUrl, setJoinUrl] = React.useState<string>("");
  const [webcam, setWebcam] = React.useState<boolean>(false);
  const [mic, setMic] = React.useState<boolean>(false);

  const [appUrl, setAppUrl] = React.useState<string>("");

  const save = React.useCallback(() => {
    if (joinUrl === '') {
      alert('Enter meeting join URL');
      return;
    }

    const cfg: TeamsMeetingDetails =
    {
      activateAcsClientWebCam: webcam,
      activateAcsClientMic: mic,
      joinUrl: joinUrl
    };

    setAppUrl(getTeamsMeetingDetailsParam(cfg));

  }, [joinUrl]);

  return (
    <div>
      <h3>Generate URL for New Meeting</h3>
      <p>To have Headless Teams client start a meeting, the app needs a URL with the meeting details as a parameter. Generate a new one below and include it in the screen playlist.</p>
      <div>
        <Box
          component="form"
          autoComplete="off"
        >
          <TextField label="Join URL" required value={joinUrl} onChange={(e: ChangeEvent<HTMLInputElement>) => setJoinUrl(e.target.value)} />

          <FormGroup>
            <h5 style={{marginTop: 20}}>Client join options</h5>
            <p>These options will be applied automatically by joining kiosk clients:</p>
            <FormControlLabel control={<Checkbox checked={webcam} onChange={(e, c) => setWebcam(c)} />} label="Activate webcam" />
            <FormControlLabel control={<Checkbox checked={mic} onChange={(e, c) => setMic(c)} />} label="Enable microphone" />
          </FormGroup>
        </Box>

        <Button variant="outlined" onClick={save} className="btn dark">Generate Url</Button>

        {appUrl !== "" &&
          <>
            <p style={{ marginTop: 20 }}>Pass this URL to auto-join the meeting:</p>
            <FormControl fullWidth sx={{ m: 1 }}>
              <InputLabel htmlFor="outlined-adornment-amount">URL</InputLabel>
              <OutlinedInput value={appUrl} readOnly />
            </FormControl>
          </>
        }

      </div>
    </div>
  );
};
