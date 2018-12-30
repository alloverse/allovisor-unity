//
//  AllovisorUrlHandler.m
//  AllovisorNativeExtensions
//
//  Created by Nevyn Bengtsson on 12/30/18.
//  Copyright © 2018 Alloverse. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface AllovisorUrlHandler : NSObject
- (void)handleURLEvent:(NSAppleEventDescriptor*)event
    withReplyEvent:(NSAppleEventDescriptor*)replyEvent;
@end

static AllovisorUrlHandler *g_handler;
static void(*g_urlCallback)(const char *url);

extern void SetUrlCallback(void(*newCallback)(const char*))
{
    g_urlCallback = newCallback;
}

@implementation AllovisorUrlHandler

+ (void)load
{
    g_handler = [AllovisorUrlHandler new];
    
    // if +load is too early, use this instead
    //[[NSNotificationCenter defaultCenter] addObserver:g_handler selector:@selector(installEventHandler) name:NSApplicationWillFinishLaunchingNotification object:nil];
    [g_handler installEventHandler];
}

- (void)installEventHandler
{
    [[NSAppleEventManager sharedAppleEventManager]
        setEventHandler:g_handler
        andSelector:@selector(handleURLEvent:withReplyEvent:)
        forEventClass:kInternetEventClass
        andEventID:kAEGetURL
    ];
    NSLog(@"Installed URL event handler");
}

- (void)handleURLEvent:(NSAppleEventDescriptor*)event
    withReplyEvent:(NSAppleEventDescriptor*)replyEvent
{
    NSString* url = [[event paramDescriptorForKeyword:keyDirectObject]
                        stringValue];
    if(!g_urlCallback) {
        NSLog(@"WARNING: No URL handler installed yet");
        return;
    }
    NSLog(@"Handling URL: %@", url);
    g_urlCallback([url UTF8String]);
}
@end

