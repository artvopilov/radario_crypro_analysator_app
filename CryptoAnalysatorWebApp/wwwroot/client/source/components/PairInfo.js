const React = require('react');
const PropTypes = require('prop-types');
const axios = require('axios');


class PairInfo extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            pair: this.props.pair,
            seller: this.props.seller,
            buyer: this.props.buyer,
            relevance: null,
            purchasePrice: this.props.purchasePrice,
            sellPrice: this.props.sellPrice,
            spread: parseFloat(this.props.spread),
            spreadClasses: ["spread"]
        }
    }

    updateRelevance(e) {
        e.preventDefault();
        const btn = e.target;
        btn.classList.add('loading');
        axios.get(this.props.url)
            .then(response => {
                console.log("track");
                btn.classList.remove('loading');
                const respData = response.data;
                const relevance =  respData.result === "Ok" ? respData.time : respData.result;
                const purchasePrice = respData.result === "Ok" ? respData.purchasePrice : "";
                const sellPrice = respData.result === "Ok" ? respData.sellPrice : "";
                const spread = respData.result === "Ok" ? parseFloat(respData.spread.replace(',', '.')) : "";
                let spreadClasses;
                if (spread === "" || spread === this.state.spread) {
                    spreadClasses = ["spread"];
                }
                else {
                    spreadClasses = (spread > this.state.spread )? ["spread", "upp"] : ["spread", "down"]
                }

                this.setState({relevance, purchasePrice, sellPrice, spread, spreadClasses});
            });
    }

    componentWillReceiveProps(props) {
        this.setState({
            relevance: null,
            purchasePrice: props.purchasePrice,
            sellPrice: props.sellPrice,
            spread: parseFloat(props.spread),
            spreadClasses: ["spread"]
        })
    }

    render() {
        return (
            <tr>
                <td className="pair">{this.props.pair}</td>
                <td className="buy">{`${this.props.seller}:  ${this.state.purchasePrice}`}</td>
                <td className="sell">{`${this.props.buyer}:  ${this.state.sellPrice}`}</td>
                <td className={this.state.spreadClasses.join(' ')}>{this.state.spread}%</td>
                <td className="special">{this.props.isCross ? "Cross" : ""} {this.state.spread > 8 ? "Chance" : ""}</td>
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
