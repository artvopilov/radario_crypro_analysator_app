const React = require('react');

class App extends React.Component {
    constructor(props) {
        super(props);

        this.state = {

        }
    }
}

/** Функция вызывает при успешной транзакции
*!/
onTransaction() {
    axios.get('/cards').then(({data}) => {
        const cardsList = App.prepareCardsData(data);
        this.setState({cardsList});

        axios.get('/transactions').then(({data}) => {
            const cardHistory = App.prepareHistory(cardsList, data);
            this.setState({cardHistory});
        });
    }
});*/

module.exports = App;
