import React, { ReactNode, useMemo } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import delete_cookie from './DeleteCookie';
import { useLocalStorage } from './AuthLocalStorage';
import { api_url } from './api';

export type AuthType = {
  authed: string | null;
  login: (data: URLSearchParams) => Promise<unknown>;
  logout: () => Promise<unknown>;
  signup: (data: URLSearchParams) => Promise<unknown>;
};

const authContext = React.createContext<AuthType | null>(null);

export function AuthProvider() {
  const [authed, setAuthed] = useLocalStorage("authed", null);

  const value = useMemo(
    () => ({
      authed,
      login(data: URLSearchParams) {
        return new Promise((resolve, reject) => {

          fetch(`${api_url}/api/user/login`, {
            method: "POST",
            credentials: 'include',
            body: data,
            headers: {
              "Content-Type": "application/x-www-form-urlencoded"
            }
          }).then(
            (f_resolved) => {
              if (f_resolved.ok) {
                return f_resolved.json().then((useridentity) => {
                  setAuthed(useridentity.userName);
                  resolve("login success");
                });
              } else {
                return f_resolved.json().then((error) => {
                  reject(new Error(error.message));
                })
              }
            }
          ).catch(() => {
            reject(new Error("Database will wake up soon. Please try again."))
          });
        })
      },
      logout() {
        return new Promise((resolve, reject) => {
          fetch(`${api_url}/api/user/logout`, {
            method: 'POST',
            credentials: 'include'
          }).then((res) => {
            if (res.ok) {
              setAuthed(null);
              // todo: replace localhost with real IP address, as well as replace the cookie settings (SameSite should not be Lax)
              delete_cookie(".AspNetCore.Identity.Application", "/", "localhost");
              resolve("logout success");
            } else {
              return reject("cannot connect to API");
            }
          });
        });
      },
      signup(data: URLSearchParams) {
        return new Promise((resolve, reject) => {
          fetch(`${api_url}/api/user/signup`, {
            method: "POST",
            credentials: 'include',
            body: data,
            headers: {
              "Content-Type": "application/x-www-form-urlencoded"
            }
          }).then(
            (f_resolved) => {
              if (f_resolved.ok) {
                return f_resolved.json().then((useridentity) => {
                  setAuthed(useridentity.userName);
                  resolve("signup success");
                })
              } else {
                return f_resolved.json().then((error) => {
                  reject(new Error(error.message));
                })
              }
            }
          ).catch(() => {
            reject(new Error("Database will wake up soon. Please try again."))
          });
        });
      }
    }), [authed, setAuthed]
  );

  return <authContext.Provider value={value}>
    <Outlet />
  </authContext.Provider>;
}

export const useAuth = () => {
  return React.useContext(authContext) as AuthType;
};

type RequireAuthProps = {
  children: ReactNode;
}

export function RequireAuth({ children }: RequireAuthProps) {
  const { authed } = useAuth();
  const location = useLocation();

  return authed !== null ? (
    children
  ) : (
    <Navigate to="/login" replace state={{ path: location.pathname }} />
  );
}

type CheckAuthProps = {
  children: ReactNode;
}

export function CheckAuth({ children }: CheckAuthProps) {
  const { authed } = useAuth();
  const location = useLocation();

  return authed === null ? (
    children
  ) : (
    <Navigate to="/dashboard" replace state={{ path: location.pathname }} />
  );
}