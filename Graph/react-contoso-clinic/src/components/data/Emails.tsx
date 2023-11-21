import '../../App.css';
import { Message } from '@microsoft/microsoft-graph-types';

function Emails(props: { messages: Message[] }) {

  return (
    <>
      <ul>
        {props.messages.map((m: Message) => {
          return (<li>
              <strong>{m.subject}</strong>
              <span className="email-details">From: {m.sender?.emailAddress?.name} | Date: {m.sentDateTime}</span>
         
          </li>);
        })}

      </ul>
    </>
  );
}

export default Emails;
