import { useState } from 'react';
import '../App.css';

export default function AppTemplate(props: { user? : microsoftgraph.User, children: React.ReactNode }) {
  

  return (
    <>
      <div className="App">
        <div id="header">
          <h1>Office Dashboard {props.user &&
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
