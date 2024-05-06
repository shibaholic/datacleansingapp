import { useState } from "react"
import Button from "react-bootstrap/esm/Button";
import Card from "react-bootstrap/esm/Card";
import Form from "react-bootstrap/esm/Form";
import Spinner from "react-bootstrap/esm/Spinner";
import "../../utilities/AuthContext"
import { useAuth } from "../../utilities/AuthContext";
import { useLocation, useNavigate } from "react-router-dom";

import "./user_pageStyle.css";


const LogInPage = () => {

  const navigate = useNavigate();
  const { login } = useAuth();
  const { state } = useLocation();

  const [msg, setMsg] = useState("");
  const [loading, setLoading] = useState(false);
  const [timerPassed, setTimerPassed] = useState(false);

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const formData = new FormData(e.currentTarget);

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const data = new URLSearchParams(formData as any);

    let inputError = false;
    let inputErrorMsg = "";
    if (formData.get("username") == "") {
      inputErrorMsg += "Enter a username. ";
      inputError = true;
    }
    if (formData.get("password") == "") {
      inputErrorMsg += "Enter a password.";
      inputError = true;
    }

    if (inputError) {
      setMsg(inputErrorMsg);
      return;
    }

    setLoading(true);

    setTimeout(() => {
      setTimerPassed(true);
    }, 10_000);

    login(data).then(
      () => { // resolved
        navigate(state?.path || "/dashboard");
      }, (rejected:Error) => {
        setMsg(rejected.message);
        setLoading(false);
        setTimerPassed(false);
    });
  }

  const navToSignUp = () => {
    navigate("/signup");
  }

  return (
    <div>
      <Card className="user-form-card">
        <Card.Title className="user-form-title">Log In</Card.Title>
        <Form onSubmit={handleSubmit}>
          <Form.Group className="mb-3" controlId="formUsername">
            <Form.Label>Username</Form.Label>
            <Form.Control className = "user-form-input" type="text" name="username" placeholder="Enter username" />
          </Form.Group>

          <Form.Group className="mb-3" controlId="formPassword">
            <Form.Label>Password</Form.Label>
            <Form.Control className="user-form-input" type="password" name="password" placeholder="Enter password" />
          </Form.Group>

          <Button className="user-form-button" variant="primary" type="submit">Log In</Button>
        </Form>

        <div className="form-bottom">
          {loading ? (
            <div>
              <Spinner className="spinner" animation="border" role="status">
                <span className="visually-hidden">Loading...</span>
              </Spinner>
              {timerPassed ? ( <p>The database is waking up from sleep...</p> ) : (<></>) }
            </div>
          ) : (
            <p className="userMessage">{msg}</p>
          )}

          <p>Don't have an account?&nbsp;
            <span onClick={navToSignUp} style={{ 'cursor': 'pointer', 'color': 'blue', 'textDecoration': 'underline' }}>
              Sign up
            </span>
          </p>
        </div>

      </Card>
    </div>
  )
}

export default LogInPage;