import { Button } from "@mui/material";
import React, { useEffect } from "react";
import { Subscription, postCreateOrUpdateSubscription, getSubscriptionsConfig, WebhooksState } from '../ApiLoader'

export const WebhookAdminPage: React.FC<{ token: string }> = (props) => {

    const [subscriptionState, setSubscriptionState] = React.useState<WebhooksState | null>(null);
    const [subCreatingOrValidating, setSubCreatingOrValidating] = React.useState<boolean>(false);

    useEffect(() => {
        getSubscriptionsConfig(props.token)
            .then((subs: WebhooksState) => {
                setSubscriptionState(subs);
            }).catch(err => {
                setSubscriptionState({ subscriptions: [], targetEndpoint: "" });
                alert(err);
            })
    }, [props.token]);

    const postCreateOrUpdateSubscriptionClick = () => {
        setSubCreatingOrValidating(true);
        postCreateOrUpdateSubscription(props.token)
            .then((sub: Subscription) => {
                alert('Done');
                setSubCreatingOrValidating(false);
            }).catch(err => {
                alert(err);
                setSubCreatingOrValidating(false);
            });
    }

    return <div>
        <h1>SPOAzBlob Webhook Subscriptions</h1>
        <p>Check &amp; configure webhooks here.</p>
        {subscriptionState === null ?
            <div>Loading...</div>
            :
            <div>
                <p>URL to receive updates from Graph: '{subscriptionState.targetEndpoint}'.</p>
                {subscriptionState.subscriptions.length === 0 ?

                    <p className="font-weight-bold">No subscriptions found. Graph is not sending updates to URL.</p>
                    :
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Resource</th>
                                <th>Change Type</th>
                                <th>Expiry</th>
                            </tr>
                        </thead>
                        {subscriptionState.subscriptions.map((sub: Subscription) => {
                            return <tr>
                                <td>
                                    {sub.resource}
                                </td>
                                <td>
                                    {sub.changeType}
                                </td>
                                <td>
                                    {sub.expirationDateTime}
                                </td>
                            </tr>
                        })}
                    </table>
                }
            </div>
        }

        <div>
            {subCreatingOrValidating ?
                <p>Loading...</p>
                :
                <div>
                    <Button onClick={postCreateOrUpdateSubscriptionClick}>Create/Update Notification for Webhook</Button>
                    <p>This is also done on a scheduled task in the Azure Functions application</p>
                </div>
            }

        </div>

    </div>
}
