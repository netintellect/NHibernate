using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace VLM.DAS2.Core
{
    public abstract class NotificationObject : INotifyPropertyChanged
    {
        #region state
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region behavior
        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return;

            OnPropertyChanged(body.Member.Name);
        }

        protected virtual void OnPropertyChanged<T>(params Expression<Func<T>>[] propertyExpressions)
        {
            if (propertyExpressions == null) { throw new ArgumentNullException(nameof(propertyExpressions)); }

            foreach (var propertyExpression in propertyExpressions)
            {
                OnPropertyChanged(propertyExpression);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
