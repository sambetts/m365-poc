import React from 'react';
import ReCAPTCHA from "react-google-recaptcha";

export const Captcha: React.FC<{ onChange: Function }> = (props) => {

  const sitekey: string | undefined = process.env.REACT_APP_RECAPTCHA_SITEKEY;

  return (
    <>
      {!sitekey ?
        <div>ERROR: No site key configured for ReCAPTCHA</div>
        :
        <ReCAPTCHA
          sitekey={sitekey}
          onChange={(value: string | null) => props.onChange(value)}
        />
      }
    </>

  );
};
