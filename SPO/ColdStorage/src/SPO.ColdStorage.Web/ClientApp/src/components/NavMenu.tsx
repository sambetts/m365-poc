import React, { Component } from 'react';
import { Collapse, Container, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import './NavMenu.css';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from "@azure/msal-react";

export class NavMenu extends Component<{}, { collapsed: boolean }> {
  static displayName = NavMenu.name;

  constructor(props: any) {
    super(props);

    this.toggleNavbar = this.toggleNavbar.bind(this);
    this.state = {
      collapsed: true
    };
  }

  toggleNavbar() {
    this.setState({
      collapsed: !this.state.collapsed
    });
  }

  render() {
    return (
      <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white border-bottom box-shadow mb-3" light>
          <Container>
            <NavbarBrand tag={Link} to="/">SharePoint Online Cold Storage</NavbarBrand>
            <NavbarToggler onClick={this.toggleNavbar} className="mr-2" />
            <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!this.state.collapsed} navbar>
              <ul className="navbar-nav flex-grow">

                <AuthenticatedTemplate>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/">Browser</NavLink>
                  </NavItem>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/FindFile">File Search</NavLink>
                  </NavItem>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/FindMigrationLog">Logs</NavLink>
                  </NavItem>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/MigrationTargets">Targets</NavLink>
                  </NavItem>
                </AuthenticatedTemplate>

                <UnauthenticatedTemplate>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/">Login</NavLink>
                  </NavItem>
                </UnauthenticatedTemplate>

              </ul>
            </Collapse>
          </Container>
        </Navbar>
      </header>
    );
  }
}
