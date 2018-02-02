const React = require('react');
const axios = require('axios');

const StatusBar = require('./StatusBar');
const ActualPairs = require('./ActualPairs');
const ActualCrossesByMarket = require('./ActualCrossesByMarket');
const Tools = require('./Tools');

class App extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            crosses: [],
            crossesByMarket: [],
            pairsAndCrosses: [],
            filter: 0
        };
    }


    componentDidMount() {
        this.updatePairs(null);
    }

    updatePairs(event) {
        let btn;
        if (event !== null) {
            event.preventDefault();
            btn = event.target.lastChild;
            btn.innerText = "Loading...";
            btn.classList.add('loading');
        }
        axios.get('/api/actualpairs/')
            .then((response) => {
                if (event !== null) {
                    btn.innerText = "Update";
                    btn.classList.remove('loading');
            }
                const pairsAndCrosses = response.data.pairs.concat(response.data.crosses).sort((a, b) => {
                    return b.spread - a.spread;
                });
                //const crosses = response.data.crosses;
                const crossesByMarket = response.data.crossesbymarket;

                this.setState({pairsAndCrosses, crossesByMarket});
            });
    }

    onChangeFilter(event) {
        this.setState({
            filter: event.target.value
        })
    }

    render() {
        return (
                <div id="app">
                    <StatusBar/>
                    <Tools onChangeFilter={this.onChangeFilter.bind(this)} updatePairs={this.updatePairs.bind(this)}/>
                    <div className="tableTitle"><h1>Арбитражные пары и кросс пары</h1></div>
                    <ActualPairs data={this.state.pairsAndCrosses} filter={this.state.filter}/>
					{/*<ActualPairs data={this.state.crosses} areCrosses={true} filter={this.state.filter}/>*/}
                    <div className="tableTitle"><h1>Кросс-пары в рамках одной биржи</h1></div>
					<ActualCrossesByMarket data={this.state.crossesByMarket} areCrosses={true} filter={this.state.filter}/>
				</div>
        )
    }
}



module.exports = App;
