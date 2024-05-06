import { useNavigate } from "react-router-dom";
import AppIcon from "../../components/AppIcon";

import './landing.css';

const Landing = () => {
  // for now, the buttons just link to the user_login and user_signup pages, which when completed should navigate to /dashboard
  // however, if the user is not authenticated and tried to go to /spreadsheet:id, then they will land on the login page which afterwards will make them go to that page
  // signup complete will always go to dashboard

  const navigate = useNavigate();

  const handleLogIn = () => {
    navigate("/login"); 
  }

  const handleSignUp = () => {
    navigate("/signup");
  }

  return (
    <div>

      <nav>
        <AppIcon />
        <div className="buttons">
          <button onClick={handleLogIn}>Log In</button>
          <button onClick={handleSignUp}>Sign Up</button>
        </div>
      </nav>

      <div className="section-1-background"></div>

      <div className="section-1">
        
        <div className="section-1-content">
          <h1>
            Easy and intuitive data cleansing for your spreadsheets
          </h1>
        </div>

        {/* need to extend landing page */}
      </div>

    </div>)
}

export default Landing;