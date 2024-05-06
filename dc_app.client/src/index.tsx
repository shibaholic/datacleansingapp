import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import Dashboard from './page/dashboard/Dashboard';

import { createBrowserRouter, RouterProvider } from 'react-router-dom'

import './../node_modules/bootstrap/dist/css/bootstrap.min.css'
import { SpreadsheetPage } from './page/spreadsheetpage/SpreadsheetPage';
import { spreadsheetPageLoader } from './page/spreadsheetpage/SpreadsheetPageLoader';
import LogInPage from './page/user/user_login';
import SignUpPage from './page/user/user_signup';
import Landing from './page/landing/landing';
import { AuthProvider, RequireAuth } from './utilities/AuthContext'; // CheckAuth

{ /* comment */ }

const router = createBrowserRouter([
  {
    element: <AuthProvider  />,
    children: [
      {
        path: "/",
        element: <Landing />
      },
      {
        path: "login",
        element: <LogInPage />
      },
      {
        path: "signup",
        element:
          <SignUpPage />
      },
      {
        path: "dashboard",
        element:
          <RequireAuth>
            <Dashboard />
          </RequireAuth>,
      },
      {
        path: "spreadsheet/:id",
        element:
          <RequireAuth>
            <SpreadsheetPage />
          </RequireAuth>,
        loader: spreadsheetPageLoader
      }
    ]
  }
])

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>
);
