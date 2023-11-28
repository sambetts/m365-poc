import { useEffect, useState } from 'react';
import { BookingStaffMember } from '@microsoft/microsoft-graph-types';
import { ContosoClinicGraphLoader } from '../services/ContosoClinicGraphLoader';

export default function StaffMember(props: { businessId: string, staffMemberId: string, loader : ContosoClinicGraphLoader }) {

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

