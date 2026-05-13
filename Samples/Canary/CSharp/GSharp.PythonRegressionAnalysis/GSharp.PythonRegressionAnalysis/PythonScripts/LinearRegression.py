import json
import numpy as np
import pandas as pd
from sklearn.linear_model import LinearRegression

def linear_regression_time_series(jdict):
    # Create a DataFrame from the dictionary
    df = pd.DataFrame(jdict)

    # Convert dates to ordinal values for regression
    df['Date_ordinal'] = pd.to_datetime(df['Date']).map(pd.Timestamp.toordinal)

    # Prepare the data for linear regression
    X = df['Date_ordinal'].values.reshape(-1, 1)
    y = df['Value'].values

    # Create and fit the model
    model = LinearRegression()
    model.fit(X, y)

    # Make predictions
    df['Predicted'] = model.predict(X)
    
    # Return results
    return model.intercept_, model.coef_[0]

def main(data):
    jdict = json.loads(data)
    calc_results = linear_regression_time_series(jdict)
    return calc_results
    
# Call the function with the sample data
result = main(data)
intercept = result[0]
slope = result[1]
