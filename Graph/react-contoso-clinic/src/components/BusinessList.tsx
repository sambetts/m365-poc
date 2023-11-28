

import { BookingBusiness } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";

export function BusinessList(props: { businesses: BookingBusiness[], select: Function }) {

  return (
    <>
      <p>Where do you want to book an appointment?</p>
      <table className="table" style={{maxWidth: 400}}>
        <thead>
          <tr><th>Name</th><th></th></tr>
        </thead>
        <tbody>
          {props.businesses.map((b: BookingBusiness) => {
            return <tr key={b.id}>
              <td>{b.displayName}</td>
              <td><Button onClick={() => props.select(b)}>Select</Button></td>
            </tr>
          })
          }

        </tbody>
      </table>
    </>
  );
}
