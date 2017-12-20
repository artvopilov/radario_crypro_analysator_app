const React = require('react');
const PropTypes = require('prop-types');
const axios = require('axios');


class PairInfo extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            relevance: null
        }
    }

    updateRelevance() {
        axios.get(this.props.url)
            .then(response => {
                const respData = response.data;
                const relevance =  respData.result === "Ok" ? respData.time : respData.result;
                this.setState({relevance});
            });
    }

    render() {
        return (
            <ul className="pairInfo">
                <li className="pairName">{this.props.pair}</li>
                <li className="buy">{`${this.props.seller}:  ${this.props.purchasePrice}`}</li>
                <li className="sell">{`${this.props.buyer}:  ${this.props.sellPrice}`}</li>
                <li className="spread">{parseFloat(this.props.sellPrice) - parseFloat(this.props.purchasePrice)}</li>
                <li className="special">{this.props.isCross ? "Cross" : ""}</li>
                <button className="trackBtn" onClick={this.updateRelevance.bind(this)}>Track</button>
                <li className="relevance">{this.state.relevance}</li>
            </ul>
        )
    }
}

PairInfo.proptypes = {
    pair: PropTypes.string,
    seller: PropTypes.string,
    buyer: PropTypes.string,
    isCross: PropTypes.bool,
    purchasePrice: PropTypes.number,
    sellPrice: PropTypes.number
};


module.exports = PairInfo;
