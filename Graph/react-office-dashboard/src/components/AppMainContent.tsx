import '../App.css';
import { AuthenticatedTemplate } from '@azure/msal-react';
import { useEffect, useState } from 'react';
import Emails from './data/Emails';
import TeamsChats from './data/TeamsChats';
import OneDriveFiles from './data/OneDriveFiles';
import { SignOutButton } from './SignOutButton';
import { ExampleAppGraphLoader } from '../services/ExampleAppGraphLoader';
import { ChatMessage, DriveItem, Message, User } from '@microsoft/microsoft-graph-types';

type TabType = 'emails' | 'chats' | 'onedrive';

export default function AppMainContent(props: { loader: ExampleAppGraphLoader }) {

  const [messages, setMessages] = useState<microsoftgraph.Message[] | null>(null);
  const [chats, setChats] = useState<microsoftgraph.ChatMessage[] | null>(null);
  const [files, setFiles] = useState<microsoftgraph.DriveItem[] | null>(null);
  const [user, setUser] = useState<microsoftgraph.User | null>(null);
  const [activeTab, setActiveTab] = useState<TabType>('emails');

  useEffect(() => {

    props.loader.loadUserProfile().then((user: User) => {
      setUser(user);
    });

    props.loader.loadEmails().then((emails: Message[]) => {
      setMessages(emails);
    });

    props.loader.loadChats().then((chats: ChatMessage[]) => {
      setChats(chats);
    });

    props.loader.loadOneDriveFiles().then((files: DriveItem[]) => {
      setFiles(files);
    });

  }, [props.loader]);

  const renderTabContent = () => {
    switch (activeTab) {
      case 'emails':
        return (
          <div className="dashboard-item" id="email-list">
            <h2>Latest Emails</h2>
            {messages ?
              <Emails messages={messages} loader={props.loader} />
              :
              <p>Loading...</p>
            }
          </div>
        );
      case 'chats':
        return (
          <div className="dashboard-item" id="teams-chat">
            <h2>Teams Chats</h2>
            {chats ?
              <TeamsChats chats={chats} />
              :
              <p>Loading...</p>
            }
          </div>
        );
      case 'onedrive':
        return (
          <div className="dashboard-item" id="onedrive-files">
            <h2>OneDrive Files</h2>
            {files ?
              <OneDriveFiles files={files} loader={props.loader} />
              :
              <p>Loading...</p>
            }
          </div>
        );
      default:
        return null;
    }
  };

  return (
    <>
      <AuthenticatedTemplate>
        <div className="App">
          <div id="header">
            <div style={{ 
              display: 'flex', 
              justifyContent: 'space-between', 
              alignItems: 'center',
              maxWidth: '1200px',
              margin: '0 auto'
            }}>
              <h1 style={{ margin: 0 }}>
                Office Dashboard - {user &&
                  <>{user.displayName}</>
                }
              </h1>
              <SignOutButton />
            </div>
          </div>
          <div id="container">
            <div className="tabs-container">
              <div className="tab-buttons">
                <button 
                  className={`tab-button ${activeTab === 'emails' ? 'active' : ''}`}
                  onClick={() => setActiveTab('emails')}
                >
                  Emails
                </button>
                <button 
                  className={`tab-button ${activeTab === 'chats' ? 'active' : ''}`}
                  onClick={() => setActiveTab('chats')}
                >
                  Teams Chats
                </button>
                <button 
                  className={`tab-button ${activeTab === 'onedrive' ? 'active' : ''}`}
                  onClick={() => setActiveTab('onedrive')}
                >
                  OneDrive Files
                </button>
              </div>
              <div className="tab-content">
                {renderTabContent()}
              </div>
            </div>
          </div>
        </div>
      </AuthenticatedTemplate>
    </>
  );
}
