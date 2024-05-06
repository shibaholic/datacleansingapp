
import logoImage from './media/logo_blue_128.png';
import './AppIcon.css';

const AppIcon = () => {

  return (
    <div className="AppIcon">
      <img src={logoImage} alt="logo"></img>
      <div className="text-div">
        <h1>DataCleanser</h1>
        <p>version 0.1</p>
      </div>

    </div>
  )
}

export default AppIcon;