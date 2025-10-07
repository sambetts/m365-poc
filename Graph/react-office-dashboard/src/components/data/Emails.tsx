import '../../App.css';
import { Message } from '@microsoft/microsoft-graph-types';

function Emails(props: { messages: Message[] }) {

  return (
    <>
      <ul>
        {props.messages.map((m: Message) => {
          return (<li>
            <button type="button" style={{ 
              background: 'none', 
              border: 'none', 
              padding: 0, 
              font: 'inherit', 
              cursor: 'pointer',
              color: 'inherit',
              textAlign: 'left',
              width: '100%'
            }}>
              <strong>{m.subject}</strong>
              <span className="email-details">From: {m.sender?.emailAddress?.name} | Date: {m.sentDateTime}</span>
            </button>
          </li>);
        })}

      </ul>
    </>
  );
}

export default Emails;
