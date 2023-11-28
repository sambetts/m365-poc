

import { BookingStaffMember } from '@microsoft/microsoft-graph-types';
import React, { useEffect } from 'react';
import Form from 'react-bootstrap/Form';

export function StaffMemberPicker(props: { options: BookingStaffMember[], optionSelected : Function }) {

  
  useEffect(() => {
    // Set a default option via callback (1st item in list)
    if (props.options.length > 0) {
      props.optionSelected(props.options[0]);
    }
  }, [props.options]);

  const newVal = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const selected: BookingStaffMember = props.options[parseInt(event.target.value)];
    
    props.optionSelected(selected)
  }

  return (
    <Form.Select onChange={(event) => newVal(event)} className='form-select'>
      {props.options.map((s, idx) => {
        return <option value={idx.toString()}>{s.displayName}</option>
      })

      }
    </Form.Select>
  );
}
