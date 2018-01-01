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

    updateRelevance(e) {
        e.preventDefault();
        const btn = e.target;
        btn.classList.add('loading');
        axios.get(this.props.url)
            .then(response => {
                btn.classList.remove('loading');
                const respData = response.data;
                const relevance =  respData.result === "Ok" ? respData.time : respData.result;
                this.setState({relevance});

            });
    }

    render() {
        return (
            <tr>
                <td className="pair">{this.props.pair}</td>
                <td className="buy">{`${this.props.seller}:  ${this.props.purchasePrice}`}</td>
                <td className="sell">{`${this.props.buyer}:  ${this.props.sellPrice}`}</td>
                <td className="spread">{this.props.spread}%</td>
                <td className="special">{this.props.isCross ? "Cross" : ""} {this.props.spread > 3 ? "chance" : ""}</td>
                <td className="relevance"><button className="trackBtn" data-label="Track" onClick={e => this.updateRelevance.bind(this)(e)}>Track</button>{this.state.relevance}</td>
            </tr>
        )
    }
}

PairInfo.proptypes = {
    pair: PropTypes.string,
    seller: PropTypes.string,
    buyer: PropTypes.string,
    isCross: PropTypes.bool,
    purchasePrice: PropTypes.number,
    sellPrice: PropTypes.number,
    relevance: PropTypes.any
};


module.exports = PairInfo;
