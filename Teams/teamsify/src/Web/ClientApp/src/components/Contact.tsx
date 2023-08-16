import { Button, TextField } from '@mui/material';
import React from 'react';
import { Rings } from 'react-loader-spinner';
import { Captcha } from './Captcha';

interface EmailMessage {
  from: string;
  message: string;
}

export const Contact: React.FC<{}> = () => {

  const [emailAddress, setEmailAddress] = React.useState<string>("");
  const [message, setMessage] = React.useState<string>("");

  const [captchaValue, setCaptchaValue] = React.useState<string | null>();
  const [isLoading, setIsLoading] = React.useState<boolean>(false);
  const [emailSent, setEmailSent] = React.useState<boolean>(false);

  const onCapChange = (value: string | null) => {
    setCaptchaValue(value);
  }

  const handleError = () => {
    setIsLoading(false);
    alert('Got an error sending email. Check JavaScript console for more info.');
  }

  const validateEmail = (email: string) => {
    return String(email)
      .toLowerCase()
      .match(
        /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/
      );
  };

  const sendEmail = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {

    e.preventDefault();

    let error = false;
    if (!validateEmail(emailAddress))
      error = true;

    if (message.length === 0)
      error = true;

    if (!error) {

      const body: EmailMessage =
      {
        from: emailAddress,
        message: message
      }

      setIsLoading(true);
      fetch("/api/TeamsApp/Contact?captchaResponseOnPage=" + captchaValue, {
        mode: 'cors',
        method: "POST",
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(body)
      })
        .then(res => {
          res.text().then(sessionId => {
            if (!res.ok) {
              console.log("Got unexpected response: " + sessionId);
              handleError();
            }
            else {
              setEmailSent(true);
              setIsLoading(false);
            }
          })
        })
        .catch(err => handleError());
    }

  }
  return (
    <div>
      <h3>Contact Me</h3>
      <p>This site was written &amp; designed by <a href='https://www.linkedin.com/in/sambettscv/'>Sam Betts</a>, an MSFT employee. 
      </p>
      <p>Email me with the form below if you have any feedback:</p>

      {!emailSent ?
        <form>
          <TextField type="email" value={emailAddress}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setEmailAddress((e.target as HTMLInputElement).value)}
            label="Your email" required style={{width: 400}} />

          <div>

            <TextField multiline value={message} rows='4'
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setMessage((e.target as HTMLTextAreaElement).value)}
              placeholder="Your message" required style={{width: 400}} />
          </div>
          <Captcha onChange={onCapChange} />

          {!isLoading ?
            <Button type="submit" variant="contained" size="large" onClick={(e) => sendEmail(e)} 
              disabled={captchaValue === null || captchaValue === undefined}>Send Email</Button>
            :
            <Rings ariaLabel="loading-indicator" color='#43488F' />
          }


        </form>
        :
        <div>Email Sent!</div>
      }
    </div>
  );
};
