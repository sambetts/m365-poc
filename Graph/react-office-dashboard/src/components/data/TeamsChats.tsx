import '../../App.css';
import { ChatMessage } from '@microsoft/microsoft-graph-types';

function TeamsChats(props: { chats: ChatMessage[] }) {

  return (
    <>
      <ul>
      {props.chats.map((c: ChatMessage) => {
          return (<li>
            <a href="javascript:void(0);">
              <strong>{c.body?.content}</strong>
              <span className="email-details">From: {c.from?.user?.displayName} | Date: {c.createdDateTime}</span>
            </a>
          </li>);
        })}
      </ul>
    </>
  );
}

export default TeamsChats;
