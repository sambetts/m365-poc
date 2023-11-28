import { useEffect, useState } from 'react';
import { BookingStaffMember } from '@microsoft/microsoft-graph-types';

export default function StaffMember(props: { allStaffMembers: BookingStaffMember[], staffMemberId: string }) {

  const [loadedStaffMember, setLoadedStaffMember] = useState<BookingStaffMember | null>(null);

  useEffect(() => {
    props.allStaffMembers.forEach((m: BookingStaffMember) => 
    {
      if (m.id === props.staffMemberId) {
        setLoadedStaffMember(m);
      }
    })

    // eslint-disable-next-line
  }, []);

  return (
    <>
      {loadedStaffMember?.displayName}
    </>
  );
}

