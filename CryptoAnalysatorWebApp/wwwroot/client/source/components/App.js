const React = require('react');
const axios = require('axios');

class App extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            crosses: [],
            pairs: []
        };


    }

    componentWillMount() {
        axios.get('/api/actualpairs')
            .then((response) => {
                this.state.pairs = response.data.pairs;
                console.log(this.state.pairs[0].pair)
            });
    }

    componentDidMount() {

    }

    render() {

        return (
            <div>
                {this.state.pairs.forEach(pair => {
                    return (
                        <div>
                            {pair.pair}
                        </div>
                    )
                })}
            </div>
        )
    }
}



module.exports = App;
