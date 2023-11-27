

import React, { useEffect } from 'react';
import Form from 'react-bootstrap/Form';

export function TimeslotPicker(props: { options: Date[], optionSelected : Function }) {

  useEffect(() => {
    // Set a default option via callback (1st item in list)
    if (props.options.length > 0) {
      props.optionSelected(props.options[0]);
    }
  }, [props.options]);

  
  const newVal = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const selected: Date = props.options[parseInt(event.target.value)];
    
    props.optionSelected(selected)
  }

  return (
    <Form.Select onChange={(event) => newVal(event)}>
      {props.options.map((dt, idx) => {
        return <option value={idx.toString()}>{dt.toString()}</option>
      })

      }
    </Form.Select>
  );
}
