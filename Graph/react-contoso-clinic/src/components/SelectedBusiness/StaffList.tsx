

import { BookingStaffMember } from "@microsoft/microsoft-graph-types";
import { useEffect, useState } from "react";
import { Button } from "react-bootstrap";
import { StaffMemberPicker } from "./StaffMemberPicker";

export function StaffList(props: { allStaff: BookingStaffMember[], newStaffList: Function }) {

  const [selectedStaff, setSelectedStaff] = useState<BookingStaffMember[]>([]);
  const [selectedStaffToAdd, setSelectedStaffToAdd] = useState<BookingStaffMember | null>(null);

  const addSelectedMember = () => {
    if (selectedStaffToAdd) {
      setSelectedStaff(old => [...old, selectedStaffToAdd]);
    }
    else
      alert('Not sure who to add?')
  }

  const setSelectedStaffToAdd2 = (s: BookingStaffMember) => {
    setSelectedStaffToAdd(s);
  }

  useEffect(() => {
    // Raise event on changes
    props.newStaffList(selectedStaff);
  }, [selectedStaff]);

  const removeMember = (b: BookingStaffMember) => {
    const newList = selectedStaff.filter(item => item.id !== b.id);
    setSelectedStaff(newList)
  }

  return (
    <>
      {selectedStaff.length > 0 ?
        <table className="table" style={{maxWidth: 400}}>
          <tbody>
            {selectedStaff.map((b: BookingStaffMember) => {
              return <tr key={b.id}>
                <td>{b.displayName}</td>
                <td><Button onClick={() => removeMember(b)} className="btn btn-secondary btn-sm">Remove</Button></td>
              </tr>
            })
            }
          </tbody>
        </table>
        :
        <ul>
          <li>No staff members added to appointment</li>
        </ul>
      }

      <table>
        <tr>
          <td><StaffMemberPicker options={props.allStaff} optionSelected={setSelectedStaffToAdd} /></td>
          <td>
            <Button onClick={addSelectedMember} className="btn btn-sm">Add</Button>
          </td>
        </tr>
      </table>
    </>
  );
}
