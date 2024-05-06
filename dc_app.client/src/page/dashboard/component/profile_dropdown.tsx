import { useState } from "react";
import Image from 'react-bootstrap/Image';
3
import defaultProfilePicture from '../media/profile_picture.jpg'

import "./profile_dropdown.css"
import { useAuth } from "../../../utilities/AuthContext";
import Dropdown from "react-bootstrap/esm/Dropdown";

import { useNavigate } from "react-router-dom";

const ProfileCircle = () => {
  const [dropped, setDropped] = useState<boolean>(false);

  const handleProfileClick = () => {
    setDropped(!dropped);
  }

  const { authed, logout } = useAuth();

  return (
    <div className="profile-circle">
      <div className="profile-name-div">
        <span className="profile-name">Welcome {authed}!</span>
      </div>
      <Image onClick={handleProfileClick} className="profile-img" src={defaultProfilePicture} roundedCircle />
      <ProfileDropdown dropped={dropped} logout={logout} />
    </div>
  )
}



interface ProfileDropdownProps {
  dropped: boolean
  logout: () => Promise<unknown>
}

const ProfileDropdown = ({ dropped, logout }: ProfileDropdownProps) => {

  const navigate = useNavigate();

  const handleLogout = () => {
    logout().then(() => {
      navigate("/");
    })
  }

  return (
    dropped ?
      (
        <div className="dropdown-container">
          <Dropdown show id="dropdown-button-drop-start" className="profile-dropdown" >
            <Dropdown.Menu id="dropdown-button-drop-start">
              <Dropdown.Item onClick={handleLogout}>Log out</Dropdown.Item>
            </Dropdown.Menu>
          </Dropdown>
        </div>
      ) : ( <div></div> )
  )
}

export default ProfileCircle;