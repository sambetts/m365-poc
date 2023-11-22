import { useEffect, useState } from 'react';
import { BookingStaffMember } from '@microsoft/microsoft-graph-types';
import { ExampleAppGraphLoader } from '../services/ExampleAppGraphLoader';

export default function StaffMember(props: { businessId: string, staffMemberId: string, loader : ExampleAppGraphLoader }) {

  const [loadedStaffMember, setLoadedStaffMember] = useState<BookingStaffMember | null>(null);

  useEffect(() => {
    props.loader.loadStaffMemberById(props.businessId, props.staffMemberId).then((user: BookingStaffMember | null) => {
      setLoadedStaffMember(user);
    });

    // eslint-disable-next-line
  }, []);

  return (
    <>
      {loadedStaffMember?.displayName}
    </>
  );
}

