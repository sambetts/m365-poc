import '../App.css';
import { AuthenticatedTemplate } from '@azure/msal-react';
import { useEffect, useState } from 'react';
import Emails from './data/Emails';
import TeamsChats from './data/TeamsChats';
import { ExampleAppGraphLoader } from '../services/ExampleAppGraphLoader';
import { ChatMessage, Message, User } from '@microsoft/microsoft-graph-types';

export default function AppMainContent(props: { loader: ExampleAppGraphLoader, userLoaded: Function }) {

  const [messages, setMessages] = useState<microsoftgraph.Message[] | null>(null);
  const [chats, setChats] = useState<microsoftgraph.ChatMessage[] | null>(null);

  useEffect(() => {

    props.loader.loadUserProfile().then((user: User) => {
      props.userLoaded(user);
    });

    props.loader.loadEmails().then((emails: Message[]) => {
      setMessages(emails);
    });

    props.loader.loadChats().then((chats: ChatMessage[]) => {
      setChats(chats);
    });

  }, [props.loader]);

  return (
    <>
      <AuthenticatedTemplate>

        <div className="dashboard-item" id="email-list">
          <h2>Latest Emails</h2>
          {messages ?
            <Emails messages={messages} />
            :
            <p>Loading...</p>
          }
        </div>
        <div className="dashboard-item" id="teams-chat">
          <h2>Teams Chats</h2>
          {chats ?
            <TeamsChats chats={chats} />
            :
            <p>Loading...</p>
          }
        </div>
      </AuthenticatedTemplate>
    </>
  );
}
