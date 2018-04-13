namespace BettingBot.Source
{
    public class InformationSender
    {
        public T ReceiveInfoWith<T>(InformationSentEventHandler h) where T : InformationSender
        {
            InformationSent += h;
            return (T) this;
        }

        public event InformationSentEventHandler InformationSent;

        protected virtual void OnInformationSending(InformationSentEventArgs e) => InformationSent?.Invoke(this, e);
        protected virtual void OnInformationSending(string information) => OnInformationSending(new InformationSentEventArgs(information));
    }

    public delegate void InformationSentEventHandler(object sender, InformationSentEventArgs e);

    public class InformationSentEventArgs
    {
        public string Information { get; }

        public InformationSentEventArgs(string information)
        {
            Information = information;
        }
    }
}
