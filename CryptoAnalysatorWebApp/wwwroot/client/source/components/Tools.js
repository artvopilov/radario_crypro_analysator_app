const React = require('react');
const PropTypes = require('prop-types');;


class Tools extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <form id="tools" onSubmit={this.props.updatePairs}>
                <label>Filter: </label>
                <input type="text" onChange={event => this.props.onChangeFilter(event)}/>%
                <button>Update</button>
            </form>
        )
    }
}

Tools.proptypes = {
    updatePairs: PropTypes.func,
    onChangeFilter: PropTypes.func
};


module.exports = Tools;
