import '../App.css';
import 'bootstrap/dist/css/bootstrap.min.css';

export default function AppTemplate(props: { user? : microsoftgraph.User, children: React.ReactNode }) {
  

  return (
    <>
      <div className="App">
        <div id="header">
          <h1>Contoso Clinic {props.user &&
            <>- {props.user.displayName}</>
          }
          </h1>
        </div>
        <div id="container">
          {props.children}
        </div>
      </div>
    </>
  );
}
