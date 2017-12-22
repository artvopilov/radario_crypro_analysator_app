const React = require('react');


class StatusBar extends React.Component {
    constructor(props) {
        super(props)
    }

    render() {
        return (
            <div className="bar">Wanna some <span className="caption">MONEY?</span> U re lucky to hv got <span className="caption">HERE</span></div>
        )
    }
}


module.exports = StatusBar;
