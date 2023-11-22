import { useEffect, useState } from 'react';
import { User } from '@microsoft/microsoft-graph-types';
import { UserLoaderCache } from '../services/UserLoaderCache';

export default function AzureAdUser(props: { userId: string, loader : UserLoaderCache }) {

  const [loadedUser, setLoadedUser] = useState<User | null>(null);

  useEffect(() => {
    props.loader.loadUserProfile(props.userId).then((user: User | null) => {
      setLoadedUser(user);
    });

    // eslint-disable-next-line
  }, []);

  return (
    <>
      {loadedUser?.displayName}
    </>
  );
}

