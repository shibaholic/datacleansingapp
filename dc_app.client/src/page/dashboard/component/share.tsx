/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useState, useEffect } from 'react';

import Button from 'react-bootstrap/Button';
import Modal from 'react-bootstrap/Modal';
import InputGroup from 'react-bootstrap/InputGroup';
import ListGroup from 'react-bootstrap/ListGroup';
import Form from "react-bootstrap/esm/Form";
import Dropdown from 'react-bootstrap/Dropdown';
import DropdownButton from 'react-bootstrap/DropdownButton';

import { SpreadshMetaModel, UserHasSpreadsheet } from '../../../model/model';
import { api_url } from '../../../utilities/api';

interface SharePopupProps {
  sharePopupBool: boolean;
  setSharePopupBool: React.Dispatch<React.SetStateAction<boolean>>;
  smodel:SpreadshMetaModel;
}

const SharePopup = ({ sharePopupBool, setSharePopupBool, smodel }: SharePopupProps) => {

  const [users, setUsers] = useState<UserHasSpreadsheet[]>([]);
  const [username, setUsername] = useState<string>("");
  const [permission, /*setPermission*/] = useState<string>("Editor");
  const [errorMsg, setErrorMsg] = useState<string>("");

  const handleClose = () => {
    setSharePopupBool(false);
  }

  useEffect(() => {
    setSharePopupBool(sharePopupBool);
  }, [sharePopupBool, setSharePopupBool])

  useEffect(() => {
    fetchSharedUsers();
  }, []);

  function fetchSharedUsers() {
    fetch(`${api_url}/api/spreadsheet/${smodel.id}/users`, {
      method: "GET",
      credentials: "include"
    }).then(resolved => {
      if (!resolved.ok) {
        throw new Error("get shared spreadsheet users response not ok");
      }
      return resolved.json();
    }).then(data => {
      setUsers(data);
    });
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleUsernameChange = (e:any /*React.ChangeEvent<HTMLInputElement>*/) => {
    setUsername(e.target.value);
  };

  function handleShare(e: React.FormEvent) {
    e.preventDefault();

    if (username === "") {
      setErrorMsg("Enter a username.");
    }

    fetch(`${api_url}/api/spreadsheet/${smodel.id}/share/${username}`, {
      method: "POST",
      credentials: "include"
    }).then(resolved => {
      if (!resolved.ok) {
        throw new Error("get shared spreadsheet users response not ok");
      }
      return resolved.json();
    }).then(data => {
      if (data.success) {
        fetchSharedUsers();
      } else {
        setErrorMsg(data.message);
      }
    })
  }

  return (
    <Modal show={sharePopupBool} onHide={handleClose}>
      <Modal.Header closeButton>
        <Modal.Title>Share your spreadsheet</Modal.Title>
      </Modal.Header>

      <Modal.Body>
        <div>
          <ListGroup as="ul">
            {users.length ?  
              users.map((user: UserHasSpreadsheet, index) => {
                return (
                  <ListGroup.Item key={index} as="li">{user.username} <span style={{color:"green"} }>{user.permission}</span></ListGroup.Item>
                )
              })
              :
              <ListGroup.Item as="li">Nothing to see here</ListGroup.Item>
            }
          </ListGroup>
        </div>

        <br/>


        <InputGroup>
          <Form.Control size="sm" type="text" value={username} onChange={(e) => handleUsernameChange(e)} placeholder="Add username to share with" />
          <DropdownButton variant="outline-primary" title={permission}>
            <Dropdown.Item href="#">Editor</Dropdown.Item>
          </DropdownButton>
          <Button variant="outline-primary" onClick={(e) => handleShare(e)}>Share</Button>
        </InputGroup>

        {errorMsg === "" ? <></> : <p style={{ color: "red" }}>{errorMsg}</p>}

      </Modal.Body>

    </Modal>
  );
}

export default SharePopup;