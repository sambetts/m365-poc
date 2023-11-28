import { useEffect, useState } from 'react';
import { BookingStaffMember } from '@microsoft/microsoft-graph-types';
import { StaffMemberLoaderCache } from '../services/GraphObjectsLoaderCaches';

export default function StaffMember(props: { businessId: string, staffMemberId: string, staffLoader : StaffMemberLoaderCache }) {

  const [loadedStaffMember, setLoadedStaffMember] = useState<BookingStaffMember | null>(null);

  useEffect(() => {
    props.staffLoader.loadFromCacheOrAPI(props.staffMemberId).then((user: BookingStaffMember | null) => {
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

