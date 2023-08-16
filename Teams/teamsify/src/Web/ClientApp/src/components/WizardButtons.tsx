import React from 'react';

import Button from '@mui/material/Button';

interface Props {
  previousClicked?: Function,
  nextClicked?: Function,
  nextText?: string,
  previousText?: string,
  disabled?: boolean
}
export const WizardButtons: React.FC<Props> = (props) => {

  const nextClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => 
  {
    e.preventDefault();
    if (props.nextClicked) {
      props.nextClicked(e);
    }
  }

  const previousClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => 
  {
    if (props.previousClicked) {
      props.previousClicked(e);
    }
  }

  return (
    <div className='wizardButtons'>
      {props.previousText &&
        <Button type="submit" variant="outlined" size="large" onClick={(e) => previousClick(e)}>{props.previousText}</Button>
      }
      {props.nextText &&
        <Button type="submit" variant="contained" disabled={props.disabled} size="large" 
          onClick={(e) => nextClick(e)} style={{marginLeft: 10}}>{props.nextText}</Button>
      }
    </div>
  );
};
