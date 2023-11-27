

import { BookingStaffMember } from "@microsoft/microsoft-graph-types";
import { useEffect, useState } from "react";
import { Button } from "react-bootstrap";
import { StaffMemberPicker } from "../common/StaffMemberPicker";

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
      <table>
        <tbody>
          {selectedStaff.length > 0 ?
            <>
              {selectedStaff.map((b: BookingStaffMember) => {
                return <tr key={b.id}>
                  <td>{b.displayName}</td>
                  <td><Button onClick={() => removeMember(b)}>Remove</Button></td>
                </tr>
              })
              }
            </>
            :
            <tr>
              <td>No staff members added</td>
            </tr>
          }

          <tr>
            <td><StaffMemberPicker options={props.allStaff} optionSelected={(s: BookingStaffMember) => setSelectedStaffToAdd(s)} /></td>
            <td>
              <Button onClick={addSelectedMember}>Add</Button>
            </td>
          </tr>

        </tbody>
      </table>

    </>
  );
}
